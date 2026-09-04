using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Audit.Models;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class AuditLogTests : IClassFixture<VetPlatformApiFactory>
{
    private const string Password = "Password123!";

    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public AuditLogTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Veterinarian_With_AuditReadOwn_Only_Sees_Their_Own_Actions()
    {
        var vetAEmail = $"audit-vet-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"audit-vet-b-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, Password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, vetBEmail, RoleNames.Veterinarian, Password);

        var vetAAuth = await LoginAsync(vetAEmail);
        var vetBAuth = await LoginAsync(vetBEmail);

        var ownerAId = await CreateOwnerAsync(vetAAuth.AccessToken);
        var patientAId = await CreatePatientAsync(vetAAuth.AccessToken, ownerAId, "Firulais A");

        var ownerBId = await CreateOwnerAsync(vetBAuth.AccessToken);
        var patientBId = await CreatePatientAsync(vetBAuth.AccessToken, ownerBId, "Firulais B");

        var vetALog = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(vetAAuth.AccessToken, "/api/audit");
        Assert.Contains(vetALog, e => e.EntityType == "Patient" && e.EntityId == patientAId);
        Assert.DoesNotContain(vetALog, e => e.EntityType == "Patient" && e.EntityId == patientBId);

        var vetBLog = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(vetBAuth.AccessToken, "/api/audit");
        Assert.Contains(vetBLog, e => e.EntityType == "Patient" && e.EntityId == patientBId);
        Assert.DoesNotContain(vetBLog, e => e.EntityType == "Patient" && e.EntityId == patientAId);
    }

    [Fact]
    public async Task Administrator_With_AuditReadAll_Sees_Every_Clinic_Users_Actions()
    {
        var adminEmail = $"audit-admin-{Guid.NewGuid():N}@vetplatform.test";
        var vetEmail = $"audit-vet-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(adminEmail, RoleNames.Administrator, Password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, vetEmail, RoleNames.Veterinarian, Password);

        var adminAuth = await LoginAsync(adminEmail);
        var vetAuth = await LoginAsync(vetEmail);

        var ownerId = await CreateOwnerAsync(vetAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAuth.AccessToken, ownerId, "Firulais");

        var adminLog = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(adminAuth.AccessToken, "/api/audit");
        Assert.Contains(adminLog, e => e.EntityType == "Patient" && e.EntityId == patientId);
    }

    [Fact]
    public async Task Consultation_Finalization_And_Amendment_Appear_As_Separate_Entries()
    {
        var vetEmail = $"audit-lifecycle-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        var vetAuth = await LoginAsync(vetEmail);

        var ownerId = await CreateOwnerAsync(vetAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAuth.AccessToken, ownerId, "Firulais");

        var consultationResponse = await PostAsAuthenticatedJsonAsync(vetAuth.AccessToken, "/api/consultations", new
        {
            patientId,
            reasonForVisit = "Chequeo de auditoria",
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
            assessment = "Estable",
            plan = "Control en 6 meses",
        });
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var finalizeResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}/finalize");
        Assert.Equal(HttpStatusCode.NoContent, finalizeResponse.StatusCode);

        var log = await GetAsAuthenticatedAsync<List<AuditEntryDto>>(vetAuth.AccessToken, "/api/audit");
        Assert.Contains(log, e => e.EntityType == "Consultation" && e.EntityId == consultationId && e.Action == "Consulta creada");
        Assert.Contains(log, e => e.EntityType == "Consultation" && e.EntityId == consultationId && e.Action == "Consulta finalizada");
    }

    [Fact]
    public async Task Receptionist_Without_Audit_Permission_Is_Forbidden()
    {
        var receptionEmail = $"audit-forbidden-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(receptionEmail, RoleNames.Receptionist, Password);
        var auth = await LoginAsync(receptionEmail);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, "/api/audit")).StatusCode);
    }

    private async Task<AuthResultDto> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private async Task<Guid> CreateOwnerAsync(string accessToken)
    {
        var response = await PostAsAuthenticatedJsonAsync(accessToken, "/api/owners", new
        {
            fullName = $"Owner {Guid.NewGuid():N}",
            identificationNumber = (string?)null,
            phone = "8888-0000",
            email = $"{Guid.NewGuid():N}@owner.test",
            address = (string?)null,
            alternateContact = (string?)null,
            consentNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreatePatientAsync(string accessToken, Guid ownerId, string name)
    {
        var response = await PostAsAuthenticatedJsonAsync(accessToken, "/api/patients", new
        {
            ownerId,
            name,
            species = "Perro",
            breed = (string?)null,
            birthDate = (DateOnly?)null,
            estimatedAge = (string?)null,
            sex = "Macho",
            reproductiveStatus = (string?)null,
            color = (string?)null,
            currentWeightKg = (decimal?)null,
            microchipNumber = (string?)null,
            photoUrl = (string?)null,
            allergies = (string?)null,
            chronicDiseases = (string?)null,
            currentMedications = (string?)null,
            vaccinationStatus = (string?)null,
            dewormingStatus = (string?)null,
            status = "Activo",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<Guid>();
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

    private async Task<HttpResponseMessage> PostAsAuthenticatedAsync(string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
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
