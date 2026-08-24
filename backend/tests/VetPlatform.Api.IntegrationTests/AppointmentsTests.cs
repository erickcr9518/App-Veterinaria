using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Appointments.Models;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Api.IntegrationTests;

public class AppointmentsTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public AppointmentsTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Receptionist_Can_Create_List_And_Change_Appointment_Status()
    {
        var receptionEmail = $"agenda-recepcion-{Guid.NewGuid():N}@vetplatform.test";
        var vetEmail = $"agenda-vet-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        var (clinicId, _) = await _factory.CreateClinicUserAsync(receptionEmail, RoleNames.Receptionist, password);
        var vetUserId = await _factory.CreateClinicUserInClinicAsync(clinicId, vetEmail, RoleNames.Veterinarian, password);
        var auth = await LoginAsync(receptionEmail, password);
        var patientId = await CreatePatientAsync(auth.AccessToken);
        var startsAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(15);

        var createResponse = await PostAsAuthenticatedJsonAsync(auth.AccessToken, "/api/appointments", new
        {
            patientId,
            assignedVeterinarianUserId = vetUserId,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Control",
            reason = "Seguimiento de tratamiento",
            notes = "Traer examenes previos",
            reminderChannel = "WhatsApp",
            reminderNotes = "Confirmar 24 horas antes",
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var appointmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var list = await GetAsAuthenticatedAsync<List<AppointmentDto>>(
            auth.AccessToken,
            $"/api/appointments?fromUtc={Uri.EscapeDataString(startsAtUtc.AddHours(-1).ToString("O"))}&toUtc={Uri.EscapeDataString(startsAtUtc.AddHours(2).ToString("O"))}");
        var appointment = Assert.Single(list);
        Assert.Equal(appointmentId, appointment.Id);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(vetUserId, appointment.AssignedVeterinarianUserId);

        var statusResponse = await PostAsAuthenticatedJsonAsync(auth.AccessToken, $"/api/appointments/{appointmentId}/status", new
        {
            status = AppointmentStatus.Confirmed,
            reason = "Confirmada por telefono",
        });
        Assert.Equal(HttpStatusCode.NoContent, statusResponse.StatusCode);

        var detail = await GetAsAuthenticatedAsync<AppointmentDto>(auth.AccessToken, $"/api/appointments/{appointmentId}");
        Assert.Equal(AppointmentStatus.Confirmed, detail.Status);
        Assert.Equal(2, detail.StatusChanges.Count);
        Assert.Contains(detail.StatusChanges, change => change.ToStatus == AppointmentStatus.Scheduled);
        Assert.Contains(detail.StatusChanges, change => change.ToStatus == AppointmentStatus.Confirmed && change.Reason == "Confirmada por telefono");
    }

    [Fact]
    public async Task Veterinarian_Can_Only_Manage_Own_Appointments()
    {
        var vetAEmail = $"agenda-vet-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"agenda-vet-b-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        var (clinicId, vetAUserId) = await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, password);
        var vetBUserId = await _factory.CreateClinicUserInClinicAsync(clinicId, vetBEmail, RoleNames.Veterinarian, password);
        var vetAAuth = await LoginAsync(vetAEmail, password);
        var patientId = await CreatePatientAsync(vetAAuth.AccessToken);
        var startsAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(10);

        var createForOtherVetResponse = await PostAsAuthenticatedJsonAsync(vetAAuth.AccessToken, "/api/appointments", new
        {
            patientId,
            assignedVeterinarianUserId = vetBUserId,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Consulta",
            reason = "Intento de asignar a otro veterinario",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Forbidden, createForOtherVetResponse.StatusCode);

        var createOwnResponse = await PostAsAuthenticatedJsonAsync(vetAAuth.AccessToken, "/api/appointments", new
        {
            patientId,
            assignedVeterinarianUserId = (Guid?)null,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Consulta",
            reason = "Cita propia",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, createOwnResponse.StatusCode);
        var appointmentId = await createOwnResponse.Content.ReadFromJsonAsync<Guid>();

        var detail = await GetAsAuthenticatedAsync<AppointmentDto>(vetAAuth.AccessToken, $"/api/appointments/{appointmentId}");
        Assert.Equal(vetAUserId, detail.AssignedVeterinarianUserId);

        var updateToOtherVetResponse = await PutAsAuthenticatedJsonAsync(vetAAuth.AccessToken, $"/api/appointments/{appointmentId}", new
        {
            patientId,
            assignedVeterinarianUserId = vetBUserId,
            startsAtUtc = startsAtUtc.AddHours(1),
            endsAtUtc = startsAtUtc.AddHours(1).AddMinutes(30),
            visitType = "Consulta",
            reason = "Intento de reasignar",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Forbidden, updateToOtherVetResponse.StatusCode);
    }

    [Fact]
    public async Task Appointments_Are_Isolated_By_Clinic()
    {
        var vetAEmail = $"agenda-clinic-a-{Guid.NewGuid():N}@vetplatform.test";
        var vetBEmail = $"agenda-clinic-b-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(vetAEmail, RoleNames.Veterinarian, password);
        await _factory.CreateClinicUserAsync(vetBEmail, RoleNames.Veterinarian, password);
        var vetAAuth = await LoginAsync(vetAEmail, password);
        var vetBAuth = await LoginAsync(vetBEmail, password);
        var patientAId = await CreatePatientAsync(vetAAuth.AccessToken);
        var startsAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(9);

        var createResponse = await PostAsAuthenticatedJsonAsync(vetAAuth.AccessToken, "/api/appointments", new
        {
            patientId = patientAId,
            assignedVeterinarianUserId = (Guid?)null,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Consulta",
            reason = "Cita clinica A",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var appointmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var crossClinicRead = await GetRawAsAuthenticatedAsync(vetBAuth.AccessToken, $"/api/appointments/{appointmentId}");
        Assert.Equal(HttpStatusCode.NotFound, crossClinicRead.StatusCode);

        var crossClinicCreate = await PostAsAuthenticatedJsonAsync(vetBAuth.AccessToken, "/api/appointments", new
        {
            patientId = patientAId,
            assignedVeterinarianUserId = (Guid?)null,
            startsAtUtc,
            endsAtUtc = startsAtUtc.AddMinutes(30),
            visitType = "Consulta",
            reason = "Intento de acceso cruzado",
            notes = (string?)null,
            reminderChannel = (string?)null,
            reminderNotes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.NotFound, crossClinicCreate.StatusCode);
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
