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
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
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
    public async Task Ask_Searches_PubMed_With_The_Translated_Query_Not_The_Original_Question()
    {
        // PubMed's index is almost entirely in English - see the bug this
        // fixes: a Spanish question searched verbatim against PubMed found
        // zero articles. The handler must search with whatever the LLM
        // translates the question to, not the raw (possibly Spanish) input.
        var recordingPubMed = new RecordingPubMedClient();
        var client = CreateClientWithFakes(recordingPubMed, new FakeLlmClient(CreateFakeSynthesis(), translatedQuery: "chronic kidney disease diet cats"));
        var email = $"vetheca-translate-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "manejo dietético de la enfermedad renal crónica en gatos",
            maxResults = 5,
        });

        Assert.Equal("chronic kidney disease diet cats", recordingPubMed.LastQuery);
    }

    [Fact]
    public async Task Ask_Falls_Back_To_The_Original_Question_When_Translation_Is_Unavailable()
    {
        var recordingPubMed = new RecordingPubMedClient();
        var client = CreateClientWithFakes(recordingPubMed, new FakeLlmClient(CreateFakeSynthesis(), translatedQuery: null));
        var email = $"vetheca-notranslate-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });

        Assert.Equal("rehabilitation after TPLO in dogs", recordingPubMed.LastQuery);
    }

    [Fact]
    public async Task AnthropicLlmClient_Translates_A_Question_Into_A_Search_Query()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler("""
            {"content": [{"type": "text", "text": "chronic kidney disease diet cats"}]}
            """));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var query = await llmClient.TranslateToSearchQueryAsync(
            "manejo dietético de la enfermedad renal crónica en gatos", CancellationToken.None);

        Assert.Equal("chronic kidney disease diet cats", query);
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

    private static VethecaSynthesisDto CreateFakeSynthesis() => new()
    {
        EvidenceSufficient = true,
        Summary = "Resumen de prueba.",
        KeyFindings = new[] { "Hallazgo 1" },
        Citations = new[] { new VethecaCitationDto { Pmid = "12345678", Claim = "Afirmacion de prueba" } },
    };

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
        private readonly string? _translatedQuery;

        public FakeLlmClient(VethecaSynthesisDto synthesis, string? translatedQuery = null)
        {
            _synthesis = synthesis;
            _translatedQuery = translatedQuery;
        }

        public Task<string?> TranslateToSearchQueryAsync(string question, CancellationToken cancellationToken)
            => Task.FromResult(_translatedQuery);

        public Task<VethecaSynthesisDto?> SynthesizeAsync(string question, IReadOnlyList<PubMedArticleDto> articles, CancellationToken cancellationToken)
            => Task.FromResult<VethecaSynthesisDto?>(_synthesis);
    }

    private class RecordingPubMedClient : IPubMedClient
    {
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
        {
            LastQuery = query;
            IReadOnlyList<PubMedArticleDto> articles = new[]
            {
                new PubMedArticleDto { Pmid = "12345678", Title = "Test", Authors = "A", Journal = "J", Year = "2023", AbstractText = "Abstract", Url = "https://pubmed.ncbi.nlm.nih.gov/12345678/" },
            };
            return Task.FromResult(articles);
        }
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
