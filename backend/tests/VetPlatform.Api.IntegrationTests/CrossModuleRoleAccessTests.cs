using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Consultations.Models;
using VetPlatform.Application.Dashboard.Models;
using VetPlatform.Application.Prescriptions.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class CrossModuleRoleAccessTests : IClassFixture<VetPlatformApiFactory>
{
    private const string Password = "Password123!";

    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public CrossModuleRoleAccessTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Receptionist_Can_Use_Front_Desk_Workflows_But_Not_Clinical_Record_Workflows()
    {
        var email = $"role-reception-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(email, RoleNames.Receptionist, Password);
        var auth = await LoginAsync(email);

        var ownerId = await CreateOwnerAsync(auth.AccessToken);
        var patientId = await CreatePatientAsync(auth.AccessToken, ownerId);

        Assert.Equal(HttpStatusCode.OK, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, "/api/owners")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, "/api/patients")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, "/api/appointments")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/patients/{patientId}/consultations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/patients/{patientId}/prescriptions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/consultations/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/prescriptions/{Guid.NewGuid()}")).StatusCode);

        var consultationWrite = await CreateConsultationResponseAsync(auth.AccessToken, patientId);
        Assert.Equal(HttpStatusCode.Forbidden, consultationWrite.StatusCode);

        var prescriptionWrite = await CreatePrescriptionResponseAsync(auth.AccessToken, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, prescriptionWrite.StatusCode);
    }

    [Fact]
    public async Task Veterinarian_Can_Read_Clinical_Record_And_Write_Clinical_Drafts()
    {
        var email = $"role-vet-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(email, RoleNames.Veterinarian, Password);
        var auth = await LoginAsync(email);

        var ownerId = await CreateOwnerAsync(auth.AccessToken);
        var patientId = await CreatePatientAsync(auth.AccessToken, ownerId);

        Assert.Equal(HttpStatusCode.OK, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/patients/{patientId}/consultations")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAuthenticatedAsync(auth.AccessToken, HttpMethod.Get, $"/api/patients/{patientId}/prescriptions")).StatusCode);

        var consultationResponse = await CreateConsultationResponseAsync(auth.AccessToken, patientId);
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var consultation = await GetAsAuthenticatedAsync<ConsultationDetailDto>(auth.AccessToken, $"/api/consultations/{consultationId}");
        Assert.Equal(patientId, consultation.PatientId);
        Assert.Equal("Draft", consultation.Status);

        var prescriptionResponse = await CreatePrescriptionResponseAsync(auth.AccessToken, consultationId);
        Assert.Equal(HttpStatusCode.Created, prescriptionResponse.StatusCode);
        var prescriptionId = await prescriptionResponse.Content.ReadFromJsonAsync<Guid>();

        var prescription = await GetAsAuthenticatedAsync<PrescriptionDetailDto>(auth.AccessToken, $"/api/prescriptions/{prescriptionId}");
        Assert.Equal(patientId, prescription.PatientId);
        Assert.Equal("Draft", prescription.Status);

        var patientPrescriptions = await GetAsAuthenticatedAsync<IReadOnlyList<PrescriptionSummaryDto>>(auth.AccessToken, $"/api/patients/{patientId}/prescriptions");
        Assert.Contains(patientPrescriptions, p => p.Id == prescriptionId);
    }

    [Fact]
    public async Task Dashboard_Summary_Gates_Clinical_Drafts_By_Current_User_Permissions()
    {
        var vetEmail = $"dashboard-vet-{Guid.NewGuid():N}@vetplatform.test";
        var receptionEmail = $"dashboard-reception-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, receptionEmail, RoleNames.Receptionist, Password);

        var vetAuth = await LoginAsync(vetEmail);
        var ownerId = await CreateOwnerAsync(vetAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAuth.AccessToken, ownerId);

        var consultationResponse = await CreateConsultationResponseAsync(vetAuth.AccessToken, patientId);
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var prescriptionResponse = await CreatePrescriptionResponseAsync(vetAuth.AccessToken, consultationId);
        Assert.Equal(HttpStatusCode.Created, prescriptionResponse.StatusCode);
        var prescriptionId = await prescriptionResponse.Content.ReadFromJsonAsync<Guid>();

        var vetDashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(vetAuth.AccessToken, "/api/dashboard/summary");
        Assert.Contains(vetDashboard.DraftConsultations, c => c.Id == consultationId);
        Assert.Contains(vetDashboard.DraftPrescriptions, p => p.Id == prescriptionId);
        Assert.Contains(vetDashboard.RecentPatients, p => p.Id == patientId);

        var receptionAuth = await LoginAsync(receptionEmail);
        var receptionDashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(receptionAuth.AccessToken, "/api/dashboard/summary");
        Assert.Empty(receptionDashboard.DraftConsultations);
        Assert.Empty(receptionDashboard.DraftPrescriptions);
        Assert.Contains(receptionDashboard.RecentPatients, p => p.Id == patientId);
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

    private async Task<Guid> CreatePatientAsync(string accessToken, Guid ownerId)
    {
        var response = await PostAsAuthenticatedJsonAsync(accessToken, "/api/patients", new
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<HttpResponseMessage> CreateConsultationResponseAsync(string accessToken, Guid patientId)
    {
        return await PostAsAuthenticatedJsonAsync(accessToken, "/api/consultations", new
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
    }

    private async Task<HttpResponseMessage> CreatePrescriptionResponseAsync(string accessToken, Guid consultationId)
    {
        return await PostAsAuthenticatedJsonAsync(accessToken, "/api/prescriptions", new
        {
            consultationId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = "Administrar con alimento",
            warnings = (string?)null,
            items = new[]
            {
                new
                {
                    productName = "Meloxicam",
                    concentration = "1.5 mg/ml",
                    presentation = "Suspension oral",
                    quantity = "1 frasco",
                    route = "Oral",
                    frequency = "Cada 24 horas",
                    duration = "5 dias",
                    instructions = (string?)null,
                },
            },
        });
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
