using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Consultations.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class ConsultationsTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public ConsultationsTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Veterinarian_Can_Create_Update_And_Finalize_A_Consultation()
    {
        var vetEmail = $"vet-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, password);
        var vetAuth = await LoginAsync(vetEmail, password);

        var patientId = await CreatePatientAsync(vetAuth.AccessToken);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAuth.AccessToken, "/api/consultations", new
        {
            patientId,
            reasonForVisit = "Vomito desde ayer",
            historyOfPresentIllness = "Comenzo con vomito hace 24 horas",
            physicalExamFindings = (string?)null,
            temperatureCelsius = 38.5,
            heartRateBpm = 100,
            respiratoryRateRpm = 24,
            weightKg = 12.8,
            diagnosticPlan = (string?)null,
            treatment = (string?)null,
            recommendations = (string?)null,
            followUpDate = (DateOnly?)null,
            subjective = "Dueno reporta vomito",
            objective = "Abdomen blando, sin dolor",
            assessment = (string?)null,
            plan = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var consultationId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var detail = await GetAsAuthenticatedAsync<ConsultationDetailDto>(vetAuth.AccessToken, $"/api/consultations/{consultationId}");
        Assert.Equal("Draft", detail.Status);
        Assert.Equal(12.8m, detail.WeightKg);

        var finalizeBeforeCompleteResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}/finalize");
        Assert.Equal(HttpStatusCode.BadRequest, finalizeBeforeCompleteResponse.StatusCode);

        var updateResponse = await PutAsAuthenticatedJsonAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}", new
        {
            reasonForVisit = "Vomito desde ayer",
            historyOfPresentIllness = "Comenzo con vomito hace 24 horas",
            physicalExamFindings = "Abdomen blando",
            temperatureCelsius = 38.5,
            heartRateBpm = 100,
            respiratoryRateRpm = 24,
            weightKg = 12.8,
            diagnosticPlan = "Panel gastrointestinal",
            treatment = "Fluidoterapia",
            recommendations = "Dieta blanda por 3 dias",
            followUpDate = (DateOnly?)null,
            subjective = "Dueno reporta vomito",
            objective = "Abdomen blando, sin dolor",
            assessment = "Gastroenteritis aguda sospechada",
            plan = "Control en 48 horas si no mejora",
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var finalizeResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}/finalize");
        Assert.Equal(HttpStatusCode.NoContent, finalizeResponse.StatusCode);

        var finalized = await GetAsAuthenticatedAsync<ConsultationDetailDto>(vetAuth.AccessToken, $"/api/consultations/{consultationId}");
        Assert.Equal("Finalized", finalized.Status);
        Assert.NotNull(finalized.FinalizedAtUtc);

        var updateAfterFinalizeResponse = await PutAsAuthenticatedJsonAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}", new
        {
            reasonForVisit = "Intento de edicion directa",
            historyOfPresentIllness = (string?)null,
            physicalExamFindings = (string?)null,
            temperatureCelsius = (decimal?)null,
            heartRateBpm = (int?)null,
            respiratoryRateRpm = (int?)null,
            weightKg = (decimal?)null,
            diagnosticPlan = (string?)null,
            treatment = (string?)null,
            recommendations = (string?)null,
            followUpDate = (DateOnly?)null,
            subjective = (string?)null,
            objective = (string?)null,
            assessment = (string?)null,
            plan = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, updateAfterFinalizeResponse.StatusCode);

        var amendResponse = await PostAsAuthenticatedJsonAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}/amend", new
        {
            reason = "Se corrige el diagnostico tras resultados de laboratorio",
            reasonForVisit = "Vomito desde ayer",
            historyOfPresentIllness = "Comenzo con vomito hace 24 horas",
            physicalExamFindings = "Abdomen blando",
            temperatureCelsius = 38.5,
            heartRateBpm = 100,
            respiratoryRateRpm = 24,
            diagnosticPlan = "Panel gastrointestinal completo",
            treatment = "Fluidoterapia y antiemetico",
            recommendations = "Dieta blanda por 5 dias",
            followUpDate = (DateOnly?)null,
            subjective = "Dueno reporta vomito",
            objective = "Abdomen blando, sin dolor",
            assessment = "Gastroenteritis aguda confirmada por laboratorio",
            plan = "Control en 5 dias",
        });
        Assert.Equal(HttpStatusCode.NoContent, amendResponse.StatusCode);

        var amended = await GetAsAuthenticatedAsync<ConsultationDetailDto>(vetAuth.AccessToken, $"/api/consultations/{consultationId}");
        Assert.Equal("Gastroenteritis aguda confirmada por laboratorio", amended.Assessment);
        Assert.Single(amended.Amendments);
        Assert.Contains("laboratorio", amended.Amendments[0].Reason);
        Assert.Contains("Gastroenteritis aguda sospechada", amended.Amendments[0].PreviousValuesJson);
    }

    [Fact]
    public async Task Receptionist_Cannot_Create_Consultation()
    {
        var receptionEmail = $"recepcion-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(receptionEmail, RoleNames.Receptionist, password);
        var auth = await LoginAsync(receptionEmail, password);

        var patientId = await CreatePatientAsync(auth.AccessToken);

        var response = await PostAsAuthenticatedJsonAsync(auth.AccessToken, "/api/consultations", new
        {
            patientId,
            reasonForVisit = "Control de rutina",
            historyOfPresentIllness = (string?)null,
            physicalExamFindings = (string?)null,
            temperatureCelsius = (decimal?)null,
            heartRateBpm = (int?)null,
            respiratoryRateRpm = (int?)null,
            weightKg = (decimal?)null,
            diagnosticPlan = (string?)null,
            treatment = (string?)null,
            recommendations = (string?)null,
            followUpDate = (DateOnly?)null,
            subjective = (string?)null,
            objective = (string?)null,
            assessment = (string?)null,
            plan = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Consultations_Are_Isolated_By_Clinic()
    {
        var vetAEmail = $"vet-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"vet-b-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserAsync(vetBEmail, RoleNames.Veterinarian, password);

        var vetAAuth = await LoginAsync(vetAEmail, password);
        var vetBAuth = await LoginAsync(vetBEmail, password);

        var patientAId = await CreatePatientAsync(vetAAuth.AccessToken);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAAuth.AccessToken, "/api/consultations", new
        {
            patientId = patientAId,
            reasonForVisit = "Consulta clinica A",
            historyOfPresentIllness = (string?)null,
            physicalExamFindings = (string?)null,
            temperatureCelsius = (decimal?)null,
            heartRateBpm = (int?)null,
            respiratoryRateRpm = (int?)null,
            weightKg = (decimal?)null,
            diagnosticPlan = (string?)null,
            treatment = (string?)null,
            recommendations = (string?)null,
            followUpDate = (DateOnly?)null,
            subjective = (string?)null,
            objective = (string?)null,
            assessment = (string?)null,
            plan = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var consultationAId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var crossClinicRead = await GetRawAsAuthenticatedAsync(vetBAuth.AccessToken, $"/api/consultations/{consultationAId}");
        Assert.Equal(HttpStatusCode.NotFound, crossClinicRead.StatusCode);

        var crossClinicPatientAttempt = await PostAsAuthenticatedJsonAsync(vetBAuth.AccessToken, "/api/consultations", new
        {
            patientId = patientAId,
            reasonForVisit = "Intento de acceso cruzado",
            historyOfPresentIllness = (string?)null,
            physicalExamFindings = (string?)null,
            temperatureCelsius = (decimal?)null,
            heartRateBpm = (int?)null,
            respiratoryRateRpm = (int?)null,
            weightKg = (decimal?)null,
            diagnosticPlan = (string?)null,
            treatment = (string?)null,
            recommendations = (string?)null,
            followUpDate = (DateOnly?)null,
            subjective = (string?)null,
            objective = (string?)null,
            assessment = (string?)null,
            plan = (string?)null,
        });
        Assert.Equal(HttpStatusCode.NotFound, crossClinicPatientAttempt.StatusCode);
    }

    private async Task<Guid> CreatePatientAsync(string accessToken)
    {
        var ownerResponse = await PostAsAuthenticatedJsonAsync(accessToken, "/api/owners", new
        {
            fullName = $"Owner {Guid.NewGuid():N}",
            identificationNumber = (string?)null,
            phone = "8888-0000",
            email = $"{Guid.NewGuid():N}@owner.test",
            address = (string?)null,
            alternateContact = (string?)null,
            consentNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);
        var ownerId = await ownerResponse.Content.ReadFromJsonAsync<Guid>();

        var patientResponse = await PostAsAuthenticatedJsonAsync(accessToken, "/api/patients", new
        {
            ownerId,
            name = "Firulais",
            species = "Perro",
            breed = "Mestizo",
            birthDate = (DateOnly?)null,
            estimatedAge = "2 anos",
            sex = "Macho",
            reproductiveStatus = (string?)null,
            color = (string?)null,
            currentWeightKg = 12.0,
            microchipNumber = (string?)null,
            photoUrl = (string?)null,
            allergies = (string?)null,
            chronicDiseases = (string?)null,
            currentMedications = (string?)null,
            vaccinationStatus = (string?)null,
            dewormingStatus = (string?)null,
            status = "Activo",
        });
        Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);
        return await patientResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private async Task<T> GetAsAuthenticatedAsync<T>(string accessToken, string requestUri)
    {
        var response = await GetRawAsAuthenticatedAsync(accessToken, requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpResponseMessage> GetRawAsAuthenticatedAsync(string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAsAuthenticatedAsync(string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(string accessToken, string requestUri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAsAuthenticatedJsonAsync<TBody>(string accessToken, string requestUri, TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }
}
