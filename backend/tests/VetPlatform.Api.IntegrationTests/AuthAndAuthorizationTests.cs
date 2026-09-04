using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Persistence;
using VetPlatform.Infrastructure.Persistence.Seed;

namespace VetPlatform.Api.IntegrationTests;

public class AuthAndAuthorizationTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public AuthAndAuthorizationTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_And_CurrentUser_Return_Demo_Admin_With_Expected_Permissions()
    {
        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);

        Assert.Equal(VetPlatformApiFactory.DemoAdminEmail, auth.Email);
        Assert.Equal(RoleNames.Administrator, auth.Role);
        Assert.Contains(PermissionCodes.UsersManage, auth.Permissions);
        Assert.DoesNotContain(PermissionCodes.ClinicsManage, auth.Permissions);

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(me);
        Assert.Equal(auth.UserId, me!.UserId);
        Assert.Equal(RoleNames.Administrator, me.Role);
    }

    [Fact]
    public async Task Refresh_Rotates_Token_And_Rejects_Reused_Token()
    {
        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken,
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(auth.RefreshToken, refreshed!.RefreshToken);

        var reusedTokenResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, reusedTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_Revokes_Refresh_Token()
    {
        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = auth.RefreshToken,
        });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Login_Locks_User_After_Repeated_Failed_Attempts()
    {
        var email = $"lockout-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Receptionist, password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failedResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = "WrongPassword123!",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var lockedResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_Does_Not_Disclose_Unknown_Email()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = $"missing-{Guid.NewGuid():N}@vetplatform.test",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PasswordResetRequestResultDto>();

        Assert.NotNull(result);
        Assert.Equal("Si el correo existe, enviaremos instrucciones para restablecer la contraseña.", result!.Message);
        Assert.Null(result.ResetUrl);
    }

    [Fact]
    public async Task ResetPassword_Uses_Token_And_Allows_Login_With_New_Password()
    {
        var email = $"reset-{Guid.NewGuid():N}@vetplatform.test";
        const string originalPassword = "Password123!";
        const string newPassword = "Changed123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Receptionist, originalPassword);
        var originalAuth = await LoginAsync(email, originalPassword);

        var forgotResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email,
        });

        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var forgotResult = await forgotResponse.Content.ReadFromJsonAsync<PasswordResetRequestResultDto>();
        Assert.NotNull(forgotResult);
        Assert.False(string.IsNullOrWhiteSpace(forgotResult!.ResetUrl));

        var token = GetQueryValue(forgotResult.ResetUrl!, "token");
        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword,
        });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var oldPasswordResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = originalPassword,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        var oldRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = originalAuth.RefreshToken,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);

        var newPasswordResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = newPassword,
        });
        Assert.Equal(HttpStatusCode.OK, newPasswordResponse.StatusCode);
    }

    [Fact]
    public async Task Auth_Endpoints_Return_TooManyRequests_When_Rate_Limit_Is_Exceeded()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = "2",
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                })));
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = $"missing-{Guid.NewGuid():N}@vetplatform.test",
                password = "WrongPassword123!",
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var rateLimitedResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@vetplatform.test",
            password = "WrongPassword123!",
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
    }

    [Fact]
    public async Task Clinic_Admin_Cannot_Create_Clinics()
    {
        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);

        var response = await PostAsAuthenticatedJsonAsync(auth.AccessToken, "/api/clinics", new
        {
            name = "Nueva Clinica",
            legalId = "123",
            address = "San Jose",
            phone = "+506 2222-2222",
            email = "nueva@clinic.test",
            timeZone = "America/Costa_Rica",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Platform_Admin_Can_Create_Clinics()
    {
        var email = $"platform-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreatePlatformAdministratorAsync(email, password);

        var auth = await LoginAsync(email, password);
        Assert.Contains(PermissionCodes.ClinicsManage, auth.Permissions);

        var response = await PostAsAuthenticatedJsonAsync(auth.AccessToken, "/api/clinics", new
        {
            name = "Clinica Plataforma",
            legalId = "456",
            address = "Heredia",
            phone = "+506 3333-3333",
            email = "platform@clinic.test",
            timeZone = "America/Costa_Rica",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Seeder_Removes_Stale_Role_Permissions()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Infrastructure.Identity.ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var administratorRole = await roleManager.FindByNameAsync(RoleNames.Administrator);
        Assert.NotNull(administratorRole);

        var clinicsManage = await dbContext.Permissions.SingleAsync(p => p.Code == PermissionCodes.ClinicsManage);
        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = administratorRole!.Id,
            PermissionId = clinicsManage.Id,
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await DataSeeder.SeedAsync(
            dbContext,
            roleManager,
            userManager,
            logger,
            seedDemoData: false,
            demoAdminPassword: null);

        var staleGrantExists = await dbContext.RolePermissions.AnyAsync(rp =>
            rp.RoleId == administratorRole.Id &&
            rp.PermissionId == clinicsManage.Id);

        Assert.False(staleGrantExists);
    }

    [Fact]
    public async Task Platform_Admin_Can_Create_Clinic_Admin_For_Target_Clinic()
    {
        var platformEmail = $"platform-{Guid.NewGuid():N}@vetplatform.test";
        const string platformPassword = "Password123!";
        await _factory.CreatePlatformAdministratorAsync(platformEmail, platformPassword);
        var platformAuth = await LoginAsync(platformEmail, platformPassword);

        var clinicResponse = await PostAsAuthenticatedJsonAsync(platformAuth.AccessToken, "/api/clinics", new
        {
            name = "Clinica Administrable",
            legalId = "789",
            address = "Cartago",
            phone = "+506 4444-4444",
            email = "admin-target@clinic.test",
            timeZone = "America/Costa_Rica",
        });
        Assert.Equal(HttpStatusCode.Created, clinicResponse.StatusCode);
        var clinicId = await clinicResponse.Content.ReadFromJsonAsync<Guid>();

        var adminEmail = $"clinic-admin-{Guid.NewGuid():N}@vetplatform.test";
        var createUserResponse = await PostAsAuthenticatedJsonAsync(platformAuth.AccessToken, "/api/users", new
        {
            email = adminEmail,
            password = "Password123!",
            fullName = "Clinic Admin",
            role = RoleNames.Administrator,
            clinicId,
        });

        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);

        var adminAuth = await LoginAsync(adminEmail, "Password123!");
        Assert.Equal(RoleNames.Administrator, adminAuth.Role);
        Assert.Equal(clinicId, adminAuth.ClinicId);
        Assert.DoesNotContain(PermissionCodes.ClinicsManage, adminAuth.Permissions);
    }

    [Fact]
    public async Task Users_Query_Returns_Only_Current_Clinic_Users()
    {
        var otherClinicUserEmail = $"other-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(otherClinicUserEmail, RoleNames.Receptionist);

        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserSummary>>();
        Assert.NotNull(users);
        Assert.Contains(users!, u => u.Email == VetPlatformApiFactory.DemoAdminEmail);
        Assert.DoesNotContain(users!, u => u.Email == otherClinicUserEmail);
    }

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        return auth;
    }

    private async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(
        string accessToken,
        string requestUri,
        TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    private static string GetQueryValue(string url, string key)
    {
        var uri = new Uri(url);
        var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == key)
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        throw new InvalidOperationException($"Query parameter '{key}' was not found.");
    }
}
