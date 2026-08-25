using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Dashboard.Models;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Api.IntegrationTests;

public class DashboardSummaryTests : IClassFixture<VetPlatformApiFactory>
{
    private const string Password = "Password123!";

    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardSummaryTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Dashboard_Only_Shows_The_Signed_In_Veterinarian_Own_Drafts()
    {
        var vetAEmail = $"dash-vet-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"dash-vet-b-{Guid.NewGuid():N}@vetplatform.test";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, Password);
        await _factory.CreateClinicUserInClinicAsync(clinicId, vetBEmail, RoleNames.Veterinarian, Password);

        var vetAAuth = await LoginAsync(vetAEmail);
        var vetBAuth = await LoginAsync(vetBEmail);

        var ownerId = await CreateOwnerAsync(vetAAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAAuth.AccessToken, ownerId);

        var consultationResponse = await CreateConsultationResponseAsync(vetAAuth.AccessToken, patientId);
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var prescriptionResponse = await CreatePrescriptionResponseAsync(vetAAuth.AccessToken, consultationId);
        Assert.Equal(HttpStatusCode.Created, prescriptionResponse.StatusCode);
        var prescriptionId = await prescriptionResponse.Content.ReadFromJsonAsync<Guid>();

        var vetADashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(vetAAuth.AccessToken, "/api/dashboard/summary");
        Assert.Contains(vetADashboard.DraftConsultations, c => c.Id == consultationId);
        Assert.Contains(vetADashboard.DraftPrescriptions, p => p.Id == prescriptionId);

        var vetBDashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(vetBAuth.AccessToken, "/api/dashboard/summary");
        Assert.DoesNotContain(vetBDashboard.DraftConsultations, c => c.Id == consultationId);
        Assert.DoesNotContain(vetBDashboard.DraftPrescriptions, p => p.Id == prescriptionId);
    }

    [Fact]
    public async Task Dashboard_Is_Isolated_By_Clinic()
    {
        var clinicAVetEmail = $"dash-clinic-a-{Guid.NewGuid():N}@vetplatform.test";
        var clinicBVetEmail = $"dash-clinic-b-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(clinicAVetEmail, RoleNames.Veterinarian, Password);
        await _factory.CreateClinicUserAsync(clinicBVetEmail, RoleNames.Veterinarian, Password);

        var clinicAAuth = await LoginAsync(clinicAVetEmail);
        var clinicBAuth = await LoginAsync(clinicBVetEmail);

        var ownerId = await CreateOwnerAsync(clinicAAuth.AccessToken);
        var patientId = await CreatePatientAsync(clinicAAuth.AccessToken, ownerId);

        var consultationResponse = await CreateConsultationResponseAsync(clinicAAuth.AccessToken, patientId);
        Assert.Equal(HttpStatusCode.Created, consultationResponse.StatusCode);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var clinicBDashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(clinicBAuth.AccessToken, "/api/dashboard/summary");
        Assert.DoesNotContain(clinicBDashboard.DraftConsultations, c => c.Id == consultationId);
        Assert.DoesNotContain(clinicBDashboard.RecentPatients, p => p.Id == patientId);

        var clinicADashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(clinicAAuth.AccessToken, "/api/dashboard/summary");
        Assert.Contains(clinicADashboard.DraftConsultations, c => c.Id == consultationId);
        Assert.Contains(clinicADashboard.RecentPatients, p => p.Id == patientId);
    }

    [Fact]
    public async Task Dashboard_Excludes_Finalized_Consultations_And_Prescriptions_From_Drafts()
    {
        var vetEmail = $"dash-finalize-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        var vetAuth = await LoginAsync(vetEmail);

        var ownerId = await CreateOwnerAsync(vetAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAuth.AccessToken, ownerId);

        var consultationResponse = await CreateConsultationResponseAsync(vetAuth.AccessToken, patientId);
        var consultationId = await consultationResponse.Content.ReadFromJsonAsync<Guid>();

        var prescriptionResponse = await CreatePrescriptionResponseAsync(vetAuth.AccessToken, consultationId);
        var prescriptionId = await prescriptionResponse.Content.ReadFromJsonAsync<Guid>();

        var beforeFinalize = await GetAsAuthenticatedAsync<DashboardSummaryDto>(vetAuth.AccessToken, "/api/dashboard/summary");
        Assert.Contains(beforeFinalize.DraftConsultations, c => c.Id == consultationId);
        Assert.Contains(beforeFinalize.DraftPrescriptions, p => p.Id == prescriptionId);

        var finalizePrescriptionResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/prescriptions/{prescriptionId}/finalize");
        Assert.Equal(HttpStatusCode.NoContent, finalizePrescriptionResponse.StatusCode);

        var finalizeConsultationResponse = await PostAsAuthenticatedAsync(vetAuth.AccessToken, $"/api/consultations/{consultationId}/finalize");
        Assert.Equal(HttpStatusCode.NoContent, finalizeConsultationResponse.StatusCode);

        var afterFinalize = await GetAsAuthenticatedAsync<DashboardSummaryDto>(vetAuth.AccessToken, "/api/dashboard/summary");
        Assert.DoesNotContain(afterFinalize.DraftConsultations, c => c.Id == consultationId);
        Assert.DoesNotContain(afterFinalize.DraftPrescriptions, p => p.Id == prescriptionId);
    }

    [Fact]
    public async Task Dashboard_Upcoming_Appointments_Excludes_Cancelled_And_Far_Future_Appointments()
    {
        var receptionEmail = $"dash-agenda-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(receptionEmail, RoleNames.Receptionist, Password);
        var auth = await LoginAsync(receptionEmail);

        var ownerId = await CreateOwnerAsync(auth.AccessToken);
        var patientId = await CreatePatientAsync(auth.AccessToken, ownerId);

        var soonStartsAtUtc = DateTime.UtcNow.AddHours(2);
        var soonAppointmentId = await CreateAppointmentAsync(auth.AccessToken, patientId, soonStartsAtUtc);

        var cancelledStartsAtUtc = DateTime.UtcNow.AddHours(3);
        var cancelledAppointmentId = await CreateAppointmentAsync(auth.AccessToken, patientId, cancelledStartsAtUtc);
        var cancelResponse = await PostAsAuthenticatedJsonAsync(auth.AccessToken, $"/api/appointments/{cancelledAppointmentId}/status", new
        {
            status = AppointmentStatus.Cancelled,
            reason = "El propietario reprogramo",
        });
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var farFutureStartsAtUtc = DateTime.UtcNow.AddDays(10);
        var farFutureAppointmentId = await CreateAppointmentAsync(auth.AccessToken, patientId, farFutureStartsAtUtc);

        var dashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(auth.AccessToken, "/api/dashboard/summary");

        Assert.Contains(dashboard.UpcomingAppointments, a => a.Id == soonAppointmentId);
        Assert.DoesNotContain(dashboard.UpcomingAppointments, a => a.Id == cancelledAppointmentId);
        Assert.DoesNotContain(dashboard.UpcomingAppointments, a => a.Id == farFutureAppointmentId);
    }

    [Fact]
    public async Task Dashboard_Platform_Administrator_Sees_Data_Across_Clinics()
    {
        var vetEmail = $"dash-platform-vet-{Guid.NewGuid():N}@vetplatform.test";
        var platformAdminEmail = $"dash-platform-admin-{Guid.NewGuid():N}@vetplatform.test";
        await _factory.CreateClinicUserAsync(vetEmail, RoleNames.Veterinarian, Password);
        await _factory.CreatePlatformAdministratorAsync(platformAdminEmail, Password);

        var vetAuth = await LoginAsync(vetEmail);
        var ownerId = await CreateOwnerAsync(vetAuth.AccessToken);
        var patientId = await CreatePatientAsync(vetAuth.AccessToken, ownerId);

        var platformAdminAuth = await LoginAsync(platformAdminEmail);
        var platformDashboard = await GetAsAuthenticatedAsync<DashboardSummaryDto>(platformAdminAuth.AccessToken, "/api/dashboard/summary");

        Assert.Contains(platformDashboard.RecentPatients, p => p.Id == patientId);
    }

    private async Task<Guid> CreateAppointmentAsync(string accessToken, Guid patientId, DateTime startsAtUtc)
    {
        var response = await PostAsAuthenticatedJsonAsync(accessToken, "/api/appointments", new
        {
            patientId,
            assignedVeterinarianUserId = (Guid?)null,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Consulta",
            reason = "Chequeo dashboard test",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<Guid>();
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
            assessment = "Gastroenteritis leve",
            plan = "Dieta blanda y control en 48 horas",
        });
    }

    private async Task<HttpResponseMessage> CreatePrescriptionResponseAsync(string accessToken, Guid consultationId)
    {
        return await PostAsAuthenticatedJsonAsync(accessToken, "/api/prescriptions", new
        {
            consultationId,
            weightKgAtPrescription = (decimal?)null,
            generalInstructions = (string?)null,
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

    private async Task<HttpResponseMessage> PostAsAuthenticatedAsync(string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
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
