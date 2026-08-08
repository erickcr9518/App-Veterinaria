using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Constants;

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
}
