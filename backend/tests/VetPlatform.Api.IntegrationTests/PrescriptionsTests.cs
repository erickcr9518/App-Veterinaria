using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Prescriptions.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class PrescriptionsTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public PrescriptionsTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Veterinarian_Can_Create_Update_And_Finalize_A_Prescription()
    {
        var vetEmail = $"vet-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, password);
        var vetAuth = await LoginAsync(vetEmail, password);

        var (patientId, consultationId) = await CreatePatientAndConsultationAsync(vetAuth.AccessToken);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAuth.AccessToken, "/api/prescriptions", new
        {
            consultationId,
            weightKgAtPrescription = 12.8,
            generalInstructions = "Administrar con alimento",
            warnings = "No usar en pacientes con insuficiencia renal",
            items = new[]
            {
                new
                {
                    productName = "Meloxicam",
                    concentration = "1.5 mg/ml",
                    presentation = "Suspension oral 10 ml",
                    quantity = "1 frasco",
                    route = "Oral",
                    frequency = "Cada 24 horas",
                    duration = "5 dias",
                    instructions = (string?)null,
                },
            },
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var prescriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var detail = await GetAsAuthenticatedAsync<PrescriptionDetailDto>(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}");
        Assert.Equal("Draft", detail.Status);
        Assert.Single(detail.Items);
        Assert.Equal("Meloxicam", detail.Items[0].ProductName);
        Assert.Equal(12.8m, detail.WeightKgAtPrescription);
        Assert.Equal("Perro", detail.PatientSpecies);
        Assert.False(string.IsNullOrWhiteSpace(detail.OwnerName));

        var updateResponse = await PutAsAuthenticatedJsonAsync(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}", new
        {
            weightKgAtPrescription = 12.8,
            generalInstructions = "Administrar con alimento",
            warnings = "No usar en pacientes con insuficiencia renal",
            items = new[]
            {
                new
                {
                    productName = "Meloxicam",
                    concentration = "1.5 mg/ml",
                    presentation = "Suspension oral 10 ml",
                    quantity = "1 frasco",
                    route = "Oral",
                    frequency = "Cada 24 horas",
                    duration = "5 dias",
                    instructions = (string?)null,
                },
                new
                {
                    productName = "Amoxicilina",
                    concentration = "250 mg",
                    presentation = "Tabletas",
                    quantity = "10 tabletas",
                    route = "Oral",
                    frequency = "Cada 12 horas",
                    duration = "7 dias",
                    instructions = (string?)"Completar el tratamiento aunque mejore antes",
                },
            },
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updated = await GetAsAuthenticatedAsync<PrescriptionDetailDto>(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}");
        Assert.Equal(2, updated.Items.Count);

        var finalizeResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}/finalize");
        Assert.Equal(HttpStatusCode.NoContent, finalizeResponse.StatusCode);

        var finalized = await GetAsAuthenticatedAsync<PrescriptionDetailDto>(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}");
        Assert.Equal("Finalized", finalized.Status);
        Assert.NotNull(finalized.FinalizedAtUtc);

        var updateAfterFinalizeResponse = await PutAsAuthenticatedJsonAsync(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}", new
        {
            weightKgAtPrescription = 13.0,
            generalInstructions = (string?)null,
            warnings = (string?)null,
            items = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, updateAfterFinalizeResponse.StatusCode);

        var patientHistory = await GetAsAuthenticatedAsync<IReadOnlyList<PrescriptionSummaryDto>>(vetAuth.AccessToken, $"/api/patients/{patientId}/prescriptions");
        Assert.Contains(patientHistory, p => p.Id == prescriptionId);
    }

    [Fact]
    public async Task Cannot_Finalize_A_Prescription_Without_Items()
    {
        var vetEmail = $"vet-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, password);
        var vetAuth = await LoginAsync(vetEmail, password);

        var (_, consultationId) = await CreatePatientAndConsultationAsync(vetAuth.AccessToken);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAuth.AccessToken, "/api/prescriptions", new
        {
            consultationId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = (string?)null,
            warnings = (string?)null,
            items = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var prescriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var finalizeResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}/finalize");
        Assert.Equal(HttpStatusCode.BadRequest, finalizeResponse.StatusCode);
    }

    [Fact]
    public async Task Receptionist_Cannot_Create_Prescription()
    {
        var receptionEmail = $"recepcion-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(receptionEmail, RoleNames.Receptionist, password);
        var receptionAuth = await LoginAsync(receptionEmail, password);

        var vetEmail = $"vet-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, password);
        var vetAuth = await LoginAsync(vetEmail, password);
        var (_, consultationId) = await CreatePatientAndConsultationAsync(vetAuth.AccessToken);

        var response = await PostAsAuthenticatedJsonAsync(receptionAuth.AccessToken, "/api/prescriptions", new
        {
            consultationId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = (string?)null,
            warnings = (string?)null,
            items = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prescriptions_Are_Isolated_By_Clinic()
    {
        var vetAEmail = $"vet-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"vet-b-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserAsync(vetBEmail, RoleNames.Veterinarian, password);

        var vetAAuth = await LoginAsync(vetAEmail, password);
        var vetBAuth = await LoginAsync(vetBEmail, password);

        var (_, consultationAId) = await CreatePatientAndConsultationAsync(vetAAuth.AccessToken);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAAuth.AccessToken, "/api/prescriptions", new
        {
            consultationId = consultationAId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = (string?)null,
            warnings = (string?)null,
            items = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var prescriptionAId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var crossClinicRead = await GetRawAsAuthenticatedAsync(vetBAuth.AccessToken, $"/api/prescriptions/{prescriptionAId}");
        Assert.Equal(HttpStatusCode.NotFound, crossClinicRead.StatusCode);

        var crossClinicCreateAttempt = await PostAsAuthenticatedJsonAsync(vetBAuth.AccessToken, "/api/prescriptions", new
        {
            consultationId = consultationAId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = (string?)null,
            warnings = (string?)null,
            items = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.NotFound, crossClinicCreateAttempt.StatusCode);
    }

    private async Task<(Guid PatientId, Guid ConsultationId)> CreatePatientAndConsultationAsync(string accessToken)
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
        var patientId = await patientResponse.Content.ReadFromJsonAsync<Guid>();

        var consultationResponse = await PostAsAuthenticatedJsonAsync(accessToken, "/api/consultations", new
        {
            patientId,
            reasonForVisit = "Consulta de rutina",
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
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        return (patientId, consultationId);
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
