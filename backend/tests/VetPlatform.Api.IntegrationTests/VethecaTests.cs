using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Application.Vetheca.Queries.AskVetheca;
using VetPlatform.Domain.Constants;
using VetPlatform.Infrastructure.Vetheca;

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
        var client = CreateClientWithFakes(new FakePubMedClient(), llmClient: null);
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
        var result = await response.Content.ReadFromJsonAsync<AskVethecaResult>();
        var article = Assert.Single(result!.Articles);
        Assert.Equal("12345678", article.Pmid);
        Assert.Equal("https://pubmed.ncbi.nlm.nih.gov/12345678/", article.Url);
        // No fake ILlmClient registered, and the test host has no Anthropic:ApiKey
        // configured either way - synthesis should gracefully come back null.
        Assert.Null(result.Synthesis);
    }

    [Fact]
    public async Task Ask_Includes_Synthesis_When_An_Llm_Client_Is_Available()
    {
        var fakeSynthesis = new VethecaSynthesisDto
        {
            EvidenceSufficient = true,
            Summary = "Resumen de prueba.",
            KeyFindings = new[] { "Hallazgo 1" },
            Citations = new[] { new VethecaCitationDto { Pmid = "12345678", Claim = "Afirmacion de prueba" } },
        };
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(fakeSynthesis));
        var email = $"vetheca-vet-synth-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var response = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AskVethecaResult>();
        Assert.NotNull(result!.Synthesis);
        Assert.True(result.Synthesis!.EvidenceSufficient);
        Assert.Equal("Resumen de prueba.", result.Synthesis.Summary);
        var citation = Assert.Single(result.Synthesis.Citations);
        Assert.Equal("12345678", citation.Pmid);
    }

    [Fact]
    public async Task Receptionist_Without_Vetheca_Permission_Is_Forbidden()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), llmClient: null);
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
        var client = CreateClientWithFakes(new FakePubMedClient(), llmClient: null);
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

    [Fact]
    public async Task AnthropicLlmClient_Drops_Citations_Referencing_Unknown_Pmids()
    {
        // This is the safety-critical bit: if Claude hallucinates a PMID that
        // wasn't in the articles it was actually given, that citation must
        // never reach the user. Exercises the real AnthropicLlmClient parsing
        // and grounding-filter logic against a stubbed HTTP response.
        var articles = new[]
        {
            new PubMedArticleDto { Pmid = "111", Title = "Real article", Authors = "Smith J", Journal = "Vet Surg", Year = "2020", AbstractText = "Abstract text." },
        };

        var anthropicResponseJson = """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"evidenciaSuficiente\": true, \"resumen\": \"Resumen.\", \"hallazgosPrincipales\": [\"Hallazgo\"], \"aplicabilidadClinica\": null, \"limitaciones\": null, \"citas\": [{\"pmid\": \"111\", \"afirmacion\": \"Afirmacion real\"}, {\"pmid\": \"999999\", \"afirmacion\": \"Afirmacion inventada\"}]}"
                }
              ]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(anthropicResponseJson));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", articles, CancellationToken.None);

        Assert.NotNull(synthesis);
        var citation = Assert.Single(synthesis!.Citations);
        Assert.Equal("111", citation.Pmid);
    }

    private HttpClient CreateClientWithFakes(IPubMedClient pubMedClient, ILlmClient? llmClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPubMedClient>();
                services.AddScoped(_ => pubMedClient);

                if (llmClient is not null)
                {
                    services.RemoveAll<ILlmClient>();
                    services.AddScoped(_ => llmClient);
                }
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

    private class FakeLlmClient : ILlmClient
    {
        private readonly VethecaSynthesisDto _synthesis;

        public FakeLlmClient(VethecaSynthesisDto synthesis)
        {
            _synthesis = synthesis;
        }

        public Task<VethecaSynthesisDto?> SynthesizeAsync(string question, IReadOnlyList<PubMedArticleDto> articles, CancellationToken cancellationToken)
            => Task.FromResult<VethecaSynthesisDto?>(_synthesis);
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StubHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
