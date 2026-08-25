using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class UsersTests : IClassFixture<VetPlatformApiFactory>
{
    private const string Password = "Password123!";

    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public UsersTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Administrator_Can_List_And_Deactivate_A_User_In_Their_Own_Clinic()
    {
        var adminEmail = $"users-admin-{Guid.NewGuid():N}@vetplatform.test";
        var vetEmail = $"users-vet-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(adminEmail, RoleNames.Administrator, Password);
        var vetUserId = await _factory.CreateClinicUserInClinicAsync(clinicId, vetEmail, RoleNames.Veterinarian, Password);

        var adminAuth = await LoginAsync(adminEmail);

        var list = await GetAsAuthenticatedAsync<List<UserSummary>>(adminAuth.AccessToken, "/api/users");
        Assert.Contains(list, u => u.UserId == vetUserId && u.IsActive);

        var deactivateResponse = await PostAsAuthenticatedJsonAsync(adminAuth.AccessToken, $"/api/users/{vetUserId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var listAfter = await GetAsAuthenticatedAsync<List<UserSummary>>(adminAuth.AccessToken, "/api/users");
        Assert.Contains(listAfter, u => u.UserId == vetUserId && !u.IsActive);
    }

    [Fact]
    public async Task Administrator_Cannot_Deactivate_A_User_From_Another_Clinic_Or_Themselves()
    {
        var adminAEmail = $"users-admin-a-{Guid.NewGuid():N}@vetplatform.test";
        var adminBEmail = $"users-admin-b-{Guid.NewGuid():N}@vetplatform.test";
        var (_, adminAUserId) = await _factory.CreateClinicUserAsync(adminAEmail, RoleNames.Administrator, Password);
        await _factory.CreateClinicUserAsync(adminBEmail, RoleNames.Administrator, Password);

        var adminAAuth = await LoginAsync(adminAEmail);
        var adminBAuth = await LoginAsync(adminBEmail);

        var selfDeactivateResponse = await PostAsAuthenticatedJsonAsync(adminAAuth.AccessToken, $"/api/users/{adminAUserId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.BadRequest, selfDeactivateResponse.StatusCode);

        var crossClinicResponse = await PostAsAuthenticatedJsonAsync(adminBAuth.AccessToken, $"/api/users/{adminAUserId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NotFound, crossClinicResponse.StatusCode);
    }

    [Fact]
    public async Task Platform_Administrator_Must_Choose_A_Clinic_To_List_Users_But_Can_Manage_Any_Clinic()
    {
        var vetEmail = $"users-platform-vet-{Guid.NewGuid():N}@vetplatform.test";
        var platformAdminEmail = $"users-platform-admin-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, vetUserId) = await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        await _factory.CreatePlatformAdministratorAsync(platformAdminEmail, Password);

        var platformAdminAuth = await LoginAsync(platformAdminEmail);

        var withoutClinicResponse = await SendAuthenticatedAsync(platformAdminAuth.AccessToken, HttpMethod.Get, "/api/users");
        Assert.Equal(HttpStatusCode.BadRequest, withoutClinicResponse.StatusCode);

        var scopedList = await GetAsAuthenticatedAsync<List<UserSummary>>(platformAdminAuth.AccessToken, $"/api/users?clinicId={clinicId}");
        Assert.Contains(scopedList, u => u.UserId == vetUserId);

        var deactivateResponse = await PostAsAuthenticatedJsonAsync(platformAdminAuth.AccessToken, $"/api/users/{vetUserId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
    }

    [Fact]
    public async Task Veterinarian_Cannot_Access_User_Management_Endpoints()
    {
        var vetEmail = $"users-forbidden-vet-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        var vetAuth = await LoginAsync(vetEmail);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(vetAuth.AccessToken, HttpMethod.Get, "/api/users")).StatusCode);
    }

    private async Task<AuthResultDto> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private async Task<T> GetAsAuthenticatedAsync<T>(string accessToken, string requestUri)
    {
        var response = await SendAuthenticatedAsync(accessToken, HttpMethod.Get, requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(string accessToken, HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(string accessToken, string requestUri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }
}
