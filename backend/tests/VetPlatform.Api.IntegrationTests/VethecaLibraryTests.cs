using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class VethecaLibraryTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;

    public VethecaLibraryTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Veterinarian_Can_Upload_A_Pdf_And_See_It_Listed()
    {
        var client = _factory.CreateClient();
        var email = $"vetheca-lib-upload-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var pdfBytes = BuildSinglePagePdf("Manejo del dolor postoperatorio en caninos y felinos.");

        var uploadResponse = await UploadAsync(client, auth.AccessToken, "Manual de analgesia", "analgesia.pdf", pdfBytes);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<VethecaLibraryDocumentDto>();
        Assert.Equal("Manual de analgesia", uploaded!.Title);
        Assert.Equal(1, uploaded.PageCount);

        var listResponse = await GetRawAsAuthenticatedAsync(client, auth.AccessToken, "/api/vetheca/library");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var documents = await listResponse.Content.ReadFromJsonAsync<List<VethecaLibraryDocumentDto>>();
        Assert.Contains(documents!, d => d.Id == uploaded.Id);
    }

    [Fact]
    public async Task Upload_Rejects_A_File_That_Is_Not_A_Valid_Pdf()
    {
        var client = _factory.CreateClient();
        var email = $"vetheca-lib-badfile-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var notAPdf = "esto no es un pdf"u8.ToArray();

        var response = await UploadAsync(client, auth.AccessToken, "Documento inválido", "no-es-pdf.pdf", notAPdf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_Clinic_Colleague_Can_See_And_Delete_A_Document_Someone_Else_Uploaded()
    {
        var client = _factory.CreateClient();
        var uploaderEmail = $"vetheca-lib-uploader-{Guid.NewGuid():N}@vetplatform.test";
        var colleagueEmail = $"vetheca-lib-colleague-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(uploaderEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, colleagueEmail, RoleNames.Administrator, password);

        var uploaderAuth = await LoginAsync(client, uploaderEmail, password);
        var colleagueAuth = await LoginAsync(client, colleagueEmail, password);

        var pdfBytes = BuildSinglePagePdf("Protocolo interno de vacunacion felina.");
        var uploadResponse = await UploadAsync(client, uploaderAuth.AccessToken, "Protocolo de vacunación", "vacunacion.pdf", pdfBytes);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<VethecaLibraryDocumentDto>();

        // Library documents are a shared clinic resource, not private to the
        // uploader - a colleague should see it and be able to remove it too.
        var listResponse = await GetRawAsAuthenticatedAsync(client, colleagueAuth.AccessToken, "/api/vetheca/library");
        var documents = await listResponse.Content.ReadFromJsonAsync<List<VethecaLibraryDocumentDto>>();
        Assert.Contains(documents!, d => d.Id == uploaded!.Id);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/vetheca/library/{uploaded!.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", colleagueAuth.AccessToken);
        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAfterDelete = await GetRawAsAuthenticatedAsync(client, uploaderAuth.AccessToken, "/api/vetheca/library");
        var documentsAfterDelete = await listAfterDelete.Content.ReadFromJsonAsync<List<VethecaLibraryDocumentDto>>();
        Assert.DoesNotContain(documentsAfterDelete!, d => d.Id == uploaded.Id);
    }

    [Fact]
    public async Task A_Clinic_Cannot_See_Another_Clinics_Library_Documents()
    {
        var client = _factory.CreateClient();
        var ownEmail = $"vetheca-lib-tenant-own-{Guid.NewGuid():N}@vetplatform.test";
        var otherEmail = $"vetheca-lib-tenant-other-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(ownEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserAsync(otherEmail, RoleNames.Veterinarian, password);

        var ownAuth = await LoginAsync(client, ownEmail, password);
        var otherAuth = await LoginAsync(client, otherEmail, password);

        var pdfBytes = BuildSinglePagePdf("Contenido exclusivo de la otra clinica.");
        var uploadResponse = await UploadAsync(client, otherAuth.AccessToken, "Solo de la otra clínica", "otra-clinica.pdf", pdfBytes);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<VethecaLibraryDocumentDto>();

        var listResponse = await GetRawAsAuthenticatedAsync(client, ownAuth.AccessToken, "/api/vetheca/library");
        var documents = await listResponse.Content.ReadFromJsonAsync<List<VethecaLibraryDocumentDto>>();

        Assert.DoesNotContain(documents!, d => d.Id == uploaded!.Id);
    }

    [Fact]
    public async Task Ask_Searches_The_Clinics_Own_Library_And_Passes_Relevant_Chunks_To_The_Llm()
    {
        // Grounding/quote-verification of library citations is the real
        // AnthropicLlmClient's job and is tested directly against that class
        // in VethecaTests.cs (same pattern as the existing PMID grounding
        // tests). This test only verifies the piece that lives in the
        // handler: that a relevant uploaded document is actually found and
        // handed to the LLM client - using a recording fake to observe what
        // it received, rather than the real Anthropic API.
        var recordingLlm = new RecordingLlmClient();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPubMedClient>();
                services.AddScoped<IPubMedClient>(_ => new EmptyPubMedClient());
                services.RemoveAll<ILlmClient>();
                services.AddScoped<ILlmClient>(_ => recordingLlm);
            });
        }).CreateClient();

        var email = $"vetheca-lib-ask-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(client, email, password);

        var pageText = "Dosis recomendada de meloxicam en felinos con osteoartritis cronica es 0.05 mg por kilogramo.";
        var pdfBytes = BuildSinglePagePdf(pageText);
        await UploadAsync(client, auth.AccessToken, "Manual de dosis felinas", "dosis.pdf", pdfBytes);

        var askResponse = await PostAsAuthenticatedJsonAsync(client, auth.AccessToken, "/api/vetheca/ask", new { question = "meloxicam dosis felinos osteoartritis", maxResults = 5 });
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);

        var excerpt = Assert.Single(recordingLlm.LastLibraryExcerpts!);
        Assert.Equal("Manual de dosis felinas", excerpt.DocumentTitle);
        Assert.Equal(1, excerpt.PageNumber);
        Assert.Equal(pageText, excerpt.Text);
    }

    [Fact]
    public async Task Ask_Does_Not_Search_The_Library_Of_A_Different_Clinic()
    {
        var recordingLlm = new RecordingLlmClient();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Uses a PubMed result (not EmptyPubMedClient) so synthesis
                // always runs regardless of the library search outcome -
                // otherwise a correctly-empty library search and "never
                // called the LLM at all" would be indistinguishable here.
                services.RemoveAll<IPubMedClient>();
                services.AddScoped<IPubMedClient>(_ => new SingleArticlePubMedClient());
                services.RemoveAll<ILlmClient>();
                services.AddScoped<ILlmClient>(_ => recordingLlm);
            });
        }).CreateClient();

        var otherClinicEmail = $"vetheca-lib-other-{Guid.NewGuid():N}@vetplatform.test";
        var ownEmail = $"vetheca-lib-own-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(otherClinicEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserAsync(ownEmail, RoleNames.Veterinarian, password);

        var otherAuth = await LoginAsync(client, otherClinicEmail, password);
        var ownAuth = await LoginAsync(client, ownEmail, password);

        var pdfBytes = BuildSinglePagePdf("Meloxicam dosis felinos osteoartritis en la otra clinica.");
        await UploadAsync(client, otherAuth.AccessToken, "Manual de la otra clinica", "otra.pdf", pdfBytes);

        await PostAsAuthenticatedJsonAsync(client, ownAuth.AccessToken, "/api/vetheca/ask", new { question = "meloxicam dosis felinos osteoartritis", maxResults = 5 });

        Assert.Empty(recordingLlm.LastLibraryExcerpts!);
    }

    private class EmptyPubMedClient : IPubMedClient
    {
        public Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PubMedArticleDto>>(Array.Empty<PubMedArticleDto>());
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

    private class RecordingLlmClient : ILlmClient
    {
        public IReadOnlyList<LibraryChunkMatchDto>? LastLibraryExcerpts { get; private set; }

        public Task<string?> TranslateToSearchQueryAsync(string question, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);

        public Task<VethecaSynthesisDto?> SynthesizeAsync(
            string question, IReadOnlyList<PubMedArticleDto> articles, IReadOnlyList<LibraryChunkMatchDto> libraryExcerpts, CancellationToken cancellationToken)
        {
            LastLibraryExcerpts = libraryExcerpts;
            return Task.FromResult<VethecaSynthesisDto?>(null);
        }
    }

    private static async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(HttpClient client, string accessToken, string requestUri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static byte[] BuildSinglePagePdf(string text)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText(text, 12, new UglyToad.PdfPig.Core.PdfPoint(25, 700), font);
        return builder.Build();
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string accessToken, string title, string fileName, byte[] fileBytes)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/vetheca/library") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<AuthResultDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private static async Task<HttpResponseMessage> GetRawAsAuthenticatedAsync(HttpClient client, string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }
}
