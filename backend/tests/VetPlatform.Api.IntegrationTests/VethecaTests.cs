using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VetPlatform.Application.Audit.Models;
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

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", articles, Array.Empty<LibraryChunkMatchDto>(), CancellationToken.None);

        Assert.NotNull(synthesis);
        var citation = Assert.Single(synthesis!.Citations);
        Assert.Equal("111", citation.Pmid);
    }

    [Fact]
    public async Task AnthropicLlmClient_Marks_A_Quote_Verified_When_It_Really_Appears_In_The_Abstract()
    {
        var articles = new[]
        {
            new PubMedArticleDto { Pmid = "111", Title = "Real article", Authors = "Smith J", Journal = "Vet Surg", Year = "2020", AbstractText = "Meloxicam did not significantly alter renal function over six months." },
        };

        var anthropicResponseJson = """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"evidenciaSuficiente\": true, \"resumen\": \"Resumen.\", \"hallazgosPrincipales\": [\"Hallazgo\"], \"aplicabilidadClinica\": null, \"limitaciones\": null, \"citas\": [{\"pmid\": \"111\", \"afirmacion\": \"No afecto la funcion renal\", \"extracto\": \"did not significantly alter renal function\"}]}"
                }
              ]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(anthropicResponseJson));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", articles, Array.Empty<LibraryChunkMatchDto>(), CancellationToken.None);

        var citation = Assert.Single(synthesis!.Citations);
        Assert.True(citation.QuoteVerified);
    }

    [Fact]
    public async Task AnthropicLlmClient_Marks_A_Quote_Unverified_When_It_Does_Not_Appear_In_The_Abstract()
    {
        var articles = new[]
        {
            new PubMedArticleDto { Pmid = "111", Title = "Real article", Authors = "Smith J", Journal = "Vet Surg", Year = "2020", AbstractText = "Meloxicam did not significantly alter renal function over six months." },
        };

        var anthropicResponseJson = """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"evidenciaSuficiente\": true, \"resumen\": \"Resumen.\", \"hallazgosPrincipales\": [\"Hallazgo\"], \"aplicabilidadClinica\": null, \"limitaciones\": null, \"citas\": [{\"pmid\": \"111\", \"afirmacion\": \"Afirmacion no respaldada\", \"extracto\": \"this exact phrase is not in the abstract\"}]}"
                }
              ]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(anthropicResponseJson));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", articles, Array.Empty<LibraryChunkMatchDto>(), CancellationToken.None);

        var citation = Assert.Single(synthesis!.Citations);
        Assert.False(citation.QuoteVerified);
    }

    [Fact]
    public async Task AnthropicLlmClient_Grounds_A_Library_Citation_And_Verifies_Its_Quote()
    {
        var documentId = Guid.NewGuid();
        var libraryExcerpts = new[]
        {
            new LibraryChunkMatchDto(documentId, "Manual de dosis felinas", 1, "Dosis recomendada de meloxicam en felinos es 0.05 mg por kilogramo."),
        };

        var anthropicResponseJson = """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"evidenciaSuficiente\": true, \"resumen\": \"Resumen.\", \"hallazgosPrincipales\": [\"Hallazgo\"], \"aplicabilidadClinica\": null, \"limitaciones\": null, \"citas\": [{\"fuente\": \"biblioteca\", \"documento\": \"Manual de dosis felinas\", \"pagina\": 1, \"afirmacion\": \"La dosis es 0.05 mg/kg\", \"extracto\": \"Dosis recomendada de meloxicam en felinos es 0.05 mg por kilogramo\"}]}"
                }
              ]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(anthropicResponseJson));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", Array.Empty<PubMedArticleDto>(), libraryExcerpts, CancellationToken.None);

        var citation = Assert.Single(synthesis!.Citations);
        Assert.Equal(VethecaCitationSource.Library, citation.Source);
        Assert.Equal(documentId, citation.LibraryDocumentId);
        Assert.True(citation.QuoteVerified);
    }

    [Fact]
    public async Task AnthropicLlmClient_Drops_A_Library_Citation_That_Does_Not_Match_Any_Retrieved_Excerpt()
    {
        // Same safety property as the PMID grounding test above, applied to
        // the library source: a document/page the model wasn't actually
        // given must never reach the user as a citation.
        var libraryExcerpts = new[]
        {
            new LibraryChunkMatchDto(Guid.NewGuid(), "Manual de dosis felinas", 1, "Contenido real de la pagina uno."),
        };

        var anthropicResponseJson = """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"evidenciaSuficiente\": true, \"resumen\": \"Resumen.\", \"hallazgosPrincipales\": [\"Hallazgo\"], \"aplicabilidadClinica\": null, \"limitaciones\": null, \"citas\": [{\"fuente\": \"biblioteca\", \"documento\": \"Documento que no existe\", \"pagina\": 999, \"afirmacion\": \"Afirmacion inventada\", \"extracto\": \"texto inventado\"}]}"
                }
              ]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(anthropicResponseJson));
        var settings = Options.Create(new AnthropicSettings { ApiKey = "test-key", Model = "claude-sonnet-5", MaxTokens = 500 });
        var llmClient = new AnthropicLlmClient(httpClient, settings, NullLogger<AnthropicLlmClient>.Instance);

        var synthesis = await llmClient.SynthesizeAsync("pregunta de prueba", Array.Empty<PubMedArticleDto>(), libraryExcerpts, CancellationToken.None);

        Assert.Empty(synthesis!.Citations);
    }

    [Fact]
    public async Task PubMedClient_Extracts_The_Real_Study_Type_From_PublicationTypeList()
    {
        var efetchXml = """
            <PubmedArticleSet>
              <PubmedArticle>
                <MedlineCitation>
                  <PMID Version="1">111</PMID>
                  <Article PubModel="Print">
                    <Journal><Title>Veterinary Surgery</Title><JournalIssue><PubDate><Year>2023</Year></PubDate></JournalIssue></Journal>
                    <ArticleTitle>A randomized trial.</ArticleTitle>
                    <Abstract><AbstractText>Some abstract text.</AbstractText></Abstract>
                    <AuthorList><Author><LastName>Smith</LastName><Initials>J</Initials></Author></AuthorList>
                    <PublicationTypeList>
                      <PublicationType>Journal Article</PublicationType>
                      <PublicationType>Randomized Controlled Trial</PublicationType>
                    </PublicationTypeList>
                  </Article>
                </MedlineCitation>
              </PubmedArticle>
            </PubmedArticleSet>
            """;

        using var httpClient = new HttpClient(new PubMedStubHttpMessageHandler(efetchXml))
        {
            BaseAddress = new Uri("https://eutils.ncbi.nlm.nih.gov/entrez/eutils/"),
        };
        var settings = Options.Create(new PubMedSettings());
        var pubMedClient = new PubMedClient(httpClient, settings, NullLogger<PubMedClient>.Instance);

        var articles = await pubMedClient.SearchAsync("test query", 5, CancellationToken.None);

        var article = Assert.Single(articles);
        Assert.Equal("Randomized Controlled Trial", article.StudyType);
    }

    [Fact]
    public async Task Ask_Returns_TooManyRequests_When_The_Per_User_Rate_Limit_Is_Exceeded()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Vetheca:PermitLimit"] = "1",
                    ["RateLimiting:Vetheca:WindowSeconds"] = "60",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPubMedClient>();
                services.AddScoped(_ => (IPubMedClient)new FakePubMedClient());
            });
        }).CreateClient();

        var email = $"vetheca-ratelimit-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var firstAsk = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        Assert.Equal(HttpStatusCode.OK, firstAsk.StatusCode);

        var secondAsk = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "another question entirely", maxResults = 5 });
        Assert.Equal(HttpStatusCode.TooManyRequests, secondAsk.StatusCode);

        // The rate limit only guards the paid ask call - reading the saved
        // list back must keep working even while a user is throttled.
        var savedList = await GetRawAsAuthenticatedAsync(client, auth.AccessToken, "/api/vetheca/saved");
        Assert.Equal(HttpStatusCode.OK, savedList.StatusCode);
    }

    [Fact]
    public async Task User_Can_Submit_Feedback_On_Their_Own_Search()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var email = $"vetheca-feedback-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();

        var feedbackResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, $"/api/vetheca/{askResult!.Id}/feedback", new { helpful = false, note = "No encontró lo que buscaba" });

        Assert.Equal(HttpStatusCode.NoContent, feedbackResponse.StatusCode);
    }

    [Fact]
    public async Task User_Cannot_Submit_Feedback_On_Another_Users_Search()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var ownerEmail = $"vetheca-fb-owner-{Guid.NewGuid():N}@vetplatform.test";
        var intruderEmail = $"vetheca-fb-intruder-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(ownerEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, intruderEmail, RoleNames.Veterinarian, password);
        var ownerAuth = await LoginAsync(client, ownerEmail, password);
        var intruderAuth = await LoginAsync(client, intruderEmail, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, ownerAuth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();

        var intruderAttempt = await PostAsAuthenticatedJsonAsync(client, intruderAuth.AccessToken, $"/api/vetheca/{askResult!.Id}/feedback", new { helpful = true, note = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, intruderAttempt.StatusCode);
    }

    [Fact]
    public async Task Ask_Persists_A_Log_Entry_Visible_In_The_Audit_Log()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var email = $"vetheca-audit-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();

        var auditLog = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(client, auth.AccessToken, "/api/audit");

        var entry = Assert.Single(auditLog, e => e.EntityType == "VethecaSearchLog" && e.EntityId == askResult!.Id);
        Assert.Equal("Consulta a Vetheca", entry.Action);
        Assert.Contains("rehabilitation after TPLO in dogs", entry.Summary);
    }

    [Fact]
    public async Task Audit_Log_Summary_Shows_Feedback_Once_Submitted()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var email = $"vetheca-audit-fb-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();

        var auditLogBeforeFeedback = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(client, auth.AccessToken, "/api/audit");
        var entryBeforeFeedback = Assert.Single(auditLogBeforeFeedback, e => e.EntityType == "VethecaSearchLog" && e.EntityId == askResult!.Id);
        Assert.DoesNotContain("👎", entryBeforeFeedback.Summary);

        await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, $"/api/vetheca/{askResult!.Id}/feedback", new { helpful = false, note = "No cubrió perros pequeños" });

        var auditLogAfterFeedback = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(client, auth.AccessToken, "/api/audit");
        var entryAfterFeedback = Assert.Single(auditLogAfterFeedback, e => e.EntityType == "VethecaSearchLog" && e.EntityId == askResult.Id);
        Assert.Contains("👎", entryAfterFeedback.Summary);
        Assert.Contains("No cubrió perros pequeños", entryAfterFeedback.Summary);
    }

    [Fact]
    public async Task User_Can_Save_A_Search_And_See_It_In_Their_Saved_List()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var email = $"vetheca-save-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new
        {
            question = "rehabilitation after TPLO in dogs",
            maxResults = 5,
        });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();

        var savedBeforeSaving = await GetAsAuthenticatedAsync<List<VethecaSavedSearchSummaryDto>>(client, auth.AccessToken, "/api/vetheca/saved");
        Assert.Empty(savedBeforeSaving);

        var saveResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, $"/api/vetheca/{askResult!.Id}/save", new { title = "TPLO en perros mayores" });
        Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

        var savedAfterSaving = await GetAsAuthenticatedAsync<List<VethecaSavedSearchSummaryDto>>(client, auth.AccessToken, "/api/vetheca/saved");
        var summary = Assert.Single(savedAfterSaving);
        Assert.Equal(askResult.Id, summary.Id);
        Assert.Equal("TPLO en perros mayores", summary.Title);

        var detail = await GetAsAuthenticatedAsync<VethecaSavedSearchDetailDto>(client, auth.AccessToken, $"/api/vetheca/saved/{askResult.Id}");
        Assert.Equal("TPLO en perros mayores", detail.Title);
        Assert.NotNull(detail.Synthesis);
        Assert.Single(detail.Articles);
    }

    [Fact]
    public async Task User_Can_Unsave_A_Search_And_It_Disappears_From_The_Saved_List_But_Not_The_Audit_Log()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var email = $"vetheca-unsave-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();
        await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, $"/api/vetheca/{askResult!.Id}/save", new { title = "Para revisar despues" });

        var unsaveResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, $"/api/vetheca/{askResult.Id}/unsave", new { });
        Assert.Equal(HttpStatusCode.NoContent, unsaveResponse.StatusCode);

        var savedAfterUnsaving = await GetAsAuthenticatedAsync<List<VethecaSavedSearchSummaryDto>>(client, auth.AccessToken, "/api/vetheca/saved");
        Assert.Empty(savedAfterUnsaving);

        // Un-saving must not erase the audit trail - it just stops showing up
        // in the user's own "saved" list.
        var auditLog = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(client, auth.AccessToken, "/api/audit");
        Assert.Contains(auditLog, e => e.EntityType == "VethecaSearchLog" && e.EntityId == askResult.Id);
    }

    [Fact]
    public async Task User_Cannot_Save_Or_View_Another_Users_Search()
    {
        var client = CreateClientWithFakes(new FakePubMedClient(), new FakeLlmClient(CreateFakeSynthesis()));
        var ownerEmail = $"vetheca-owner-{Guid.NewGuid():N}@vetplatform.test";
        var intruderEmail = $"vetheca-intruder-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(ownerEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, intruderEmail, RoleNames.Veterinarian, password);
        var ownerAuth = await LoginAsync(client, ownerEmail, password);
        var intruderAuth = await LoginAsync(client, intruderEmail, password);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, ownerAuth.AccessToken, "/api/vetheca/ask", new { question = "rehabilitation after TPLO in dogs", maxResults = 5 });
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskVethecaResult>();
        await PostAsAuthenticatedJsonAsync(client, ownerAuth.AccessToken, $"/api/vetheca/{askResult!.Id}/save", new { title = "Mia" });

        var intruderSaveAttempt = await PostAsAuthenticatedJsonAsync(client, intruderAuth.AccessToken, $"/api/vetheca/{askResult.Id}/save", new { title = "Robada" });
        Assert.Equal(HttpStatusCode.Forbidden, intruderSaveAttempt.StatusCode);

        var intruderViewAttempt = await GetRawAsAuthenticatedAsync(client, intruderAuth.AccessToken, $"/api/vetheca/saved/{askResult.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, intruderViewAttempt.StatusCode);
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

    private static async Task<T> GetAsAuthenticatedAsync<T>(HttpClient client, string accessToken, string requestUri)
    {
        var response = await GetRawAsAuthenticatedAsync(client, accessToken, requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<HttpResponseMessage> GetRawAsAuthenticatedAsync(HttpClient client, string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
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

        public Task<VethecaSynthesisDto?> SynthesizeAsync(
            string question, IReadOnlyList<PubMedArticleDto> articles, IReadOnlyList<LibraryChunkMatchDto> libraryExcerpts, CancellationToken cancellationToken)
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

    // Routes esearch calls to a fixed PMID list and efetch calls to fixed
    // article XML, so PubMedClient's real parsing logic (including study
    // type extraction) runs against known input.
    private class PubMedStubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _efetchXml;

        public PubMedStubHttpMessageHandler(string efetchXml)
        {
            _efetchXml = efetchXml;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isEsearch = request.RequestUri!.AbsolutePath.Contains("esearch", StringComparison.OrdinalIgnoreCase);
            var body = isEsearch
                ? """{"esearchresult": {"idlist": ["111"]}}"""
                : _efetchXml;
            var contentType = isEsearch ? "application/json" : "application/xml";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType),
            });
        }
    }
}
