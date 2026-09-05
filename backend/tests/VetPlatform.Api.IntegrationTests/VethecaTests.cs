using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class VethecaTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;

    public VethecaTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Veterinarian_Can_Ask_Vetheca_And_Gets_Articles_From_PubMedClient()
    {
        var client = CreateClientWithFakePubMed(new FakePubMedClient());
        var email = $"vetheca-vet-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var response = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var articles = await response.Content.ReadFromJsonAsync<List<PubMedArticleDto>>();
        var article = Assert.Single(articles!);
        Assert.Equal("12345678", article.Pmid);
        Assert.Equal("https://pubmed.ncbi.nlm.nih.gov/12345678/", article.Url);
    }

    [Fact]
    public async Task Receptionist_Without_Vetheca_Permission_Is_Forbidden()
    {
        var client = CreateClientWithFakePubMed(new FakePubMedClient());
        var email = $"vetheca-recepcion-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Receptionist, password);
        var auth = await LoginAsync(client, email, password);

        var response = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Empty_Question_Returns_BadRequest()
    {
        var client = CreateClientWithFakePubMed(new FakePubMedClient());
        var email = $"vetheca-admin-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Administrator, password);
        var auth = await LoginAsync(client, email, password);

        var response = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "",
            maxResults = 5,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClientWithFakePubMed(IPubMedClient fakePubMedClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPubMedClient>();
                services.AddScoped(_ => fakePubMedClient);
            });
        }).CreateClient();
    }

    private static async Task<AuthResultDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private static async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(HttpClient client, string accessToken, string requestUri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private class FakePubMedClient : IPubMedClient
    {
        public Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
        {
            IReadOnlyList<PubMedArticleDto> articles = new[]
            {
                new PubMedArticleDto
                {
                    Pmid = "12345678",
                    Title = "Early rehabilitation after TPLO in dogs: a retrospective study",
                    Authors = "Smith J, Doe A",
                    Journal = "Veterinary Surgery",
                    Year = "2023",
                    AbstractText = "OBJECTIVE: To evaluate outcomes of early rehabilitation after TPLO.",
                    Url = "https://pubmed.ncbi.nlm.nih.gov/12345678/",
                },
            };
            return Task.FromResult(articles);
        }
    }
}
