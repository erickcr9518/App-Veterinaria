using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class VethecaQuotaTests : IClassFixture<VetPlatformApiFactory>
{
    private const string Password = "Password123!";

    private readonly VetPlatformApiFactory _factory;

    public VethecaQuotaTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Vetheca_Only_User_Can_Ask_But_Cannot_Reach_Clinical_Data()
    {
        var client = CreateClient(monthlyLimit: 100);
        var auth = await CreateAndLoginAsync(client, RoleNames.VethecaVeterinarian);

        var askResponse = await PostAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync(client, auth.AccessToken, "/api/owners")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync(client, auth.AccessToken, "/api/patients")).StatusCode);
    }

    [Fact]
    public async Task Vetheca_Only_User_Is_Blocked_After_The_Monthly_Limit_With_A_Quota_Code()
    {
        var client = CreateClient(monthlyLimit: 2);
        var auth = await CreateAndLoginAsync(client, RoleNames.VethecaVeterinarian);

        for (var i = 0; i < 2; i++)
        {
            var allowed = await PostAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = $"pregunta {i}", maxResults = 5 });
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var blocked = await PostAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "una pregunta de mas", maxResults = 5 });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var body = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("vetheca_quota_exceeded", body);

        var quota = await GetQuotaAsync(client, auth.AccessToken);
        Assert.Equal(2, quota.MonthlyLimit);
        Assert.Equal(2, quota.UsedThisMonth);
        Assert.Equal(0, quota.Remaining);

        // Resets at midnight Costa Rica time (UTC-6) on the 1st, i.e. 06:00 UTC.
        Assert.Equal(1, quota.ResetsAtUtc.Day);
        Assert.Equal(6, quota.ResetsAtUtc.Hour);
    }

    [Fact]
    public async Task Quota_Is_Counted_Per_User_Not_Per_Clinic()
    {
        var client = CreateClient(monthlyLimit: 1);
        var firstEmail = $"quota-first-{Guid.NewGuid():N}@vetplatform.test";
        var secondEmail = $"quota-second-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(firstEmail, RoleNames.VethecaVeterinarian, Password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, secondEmail, RoleNames.VethecaVeterinarian, Password);
        var first = await LoginAsync(client, firstEmail);
        var second = await LoginAsync(client, secondEmail);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, first.AccessToken, "/api/vetheca/ask", new { question = "primera", maxResults = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostAsync(client, first.AccessToken, "/api/vetheca/ask", new { question = "segunda", maxResults = 5 })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, second.AccessToken, "/api/vetheca/ask", new { question = "de la otra persona", maxResults = 5 })).StatusCode);
    }

    [Fact]
    public async Task Full_Clinic_Veterinarian_Has_No_Question_Limit()
    {
        var client = CreateClient(monthlyLimit: 1);
        var auth = await CreateAndLoginAsync(client, RoleNames.Veterinarian);

        for (var i = 0; i < 3; i++)
        {
            var response = await PostAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = $"pregunta {i}", maxResults = 5 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var quota = await GetQuotaAsync(client, auth.AccessToken);
        Assert.Null(quota.MonthlyLimit);
        Assert.Null(quota.Remaining);
    }

    private HttpClient CreateClient(int monthlyLimit)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Vetheca:Quota:VethecaVeterinarianMonthlyLimit"] = monthlyLimit.ToString(),
                    // Not under test here - keep the per-hour rate limit out of the way.
                    ["RateLimiting:Vetheca:PermitLimit"] = "1000",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPubMedClient>();
                services.AddScoped<IPubMedClient>(_ => new SingleArticlePubMedClient());
            });
        }).CreateClient();
    }

    private async Task<AuthResultDto> CreateAndLoginAsync(HttpClient client, string role)
    {
        var email = $"quota-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(email, role, Password);
        return await LoginAsync(client, email);
    }

    private static async Task<AuthResultDto> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private static async Task<VethecaQuotaDto> GetQuotaAsync(HttpClient client, string accessToken)
    {
        var response = await GetAsync(client, accessToken, "/api/vetheca/quota");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<VethecaQuotaDto>())!;
    }

    private static async Task<HttpResponseMessage> PostAsync<TBody>(HttpClient client, string accessToken, string uri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string accessToken, string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private class SingleArticlePubMedClient : IPubMedClient
    {
        public Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
        {
            IReadOnlyList<PubMedArticleDto> articles = new[]
            {
                new PubMedArticleDto { Pmid = "1", Title = "T", Authors = "A", Journal = "J", Year = "2023", AbstractText = "Abstract." },
            };
            return Task.FromResult(articles);
        }
    }
}
