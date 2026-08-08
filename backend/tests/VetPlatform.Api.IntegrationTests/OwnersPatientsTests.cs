using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Owners.Models;
using VetPlatform.Application.Patients.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.IntegrationTests;

public class OwnersPatientsTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly VetPlatformApiFactory _factory;
    private readonly HttpClient _client;

    public OwnersPatientsTests(VetPlatformApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Clinic_User_Can_Create_Owner_And_Patient()
    {
        var auth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);

        var ownerId = await CreateOwnerAsync(auth.AccessToken, "Maria Lopez", "8888-0000");
        var patientId = await CreatePatientAsync(auth.AccessToken, ownerId, "Luna");

        var owner = await GetAsAuthenticatedAsync<OwnerDto>(auth.AccessToken, $"/api/owners/{ownerId}");
        var patient = await GetAsAuthenticatedAsync<PatientDto>(auth.AccessToken, $"/api/patients/{patientId}");

        Assert.Equal("Maria Lopez", owner.FullName);
        Assert.Equal("Luna", patient.Name);
        Assert.Equal(ownerId, patient.OwnerId);
        Assert.Equal("Perro", patient.Species);
        Assert.Equal(12.4m, patient.CurrentWeightKg);
    }

    [Fact]
    public async Task Owners_And_Patients_Are_Isolated_By_Clinic()
    {
        var otherEmail = $"recepcion-{Guid.NewGuid():N}@vetplatform.test";
        const string password = "Password123!";
        await _factory.CreateClinicUserAsync(otherEmail, RoleNames.Receptionist, password);

        var demoAuth = await LoginAsync(VetPlatformApiFactory.DemoAdminEmail, VetPlatformApiFactory.DemoAdminPassword);
        var otherAuth = await LoginAsync(otherEmail, password);

        var demoOwnerId = await CreateOwnerAsync(demoAuth.AccessToken, "Carlos Demo", "7000-0000");
        var otherOwnerId = await CreateOwnerAsync(otherAuth.AccessToken, "Ana Otra", "7111-1111");
        var otherPatientId = await CreatePatientAsync(otherAuth.AccessToken, otherOwnerId, "Milo");

        var demoOwners = await GetAsAuthenticatedAsync<IReadOnlyList<OwnerDto>>(demoAuth.AccessToken, "/api/owners");
        var demoPatients = await GetAsAuthenticatedAsync<IReadOnlyList<PatientDto>>(demoAuth.AccessToken, "/api/patients");

        Assert.Contains(demoOwners, o => o.Id == demoOwnerId);
        Assert.DoesNotContain(demoOwners, o => o.Id == otherOwnerId);
        Assert.DoesNotContain(demoPatients, p => p.Id == otherPatientId);

        var crossClinicOwner = await SendAuthenticatedAsync(demoAuth.AccessToken, HttpMethod.Get, $"/api/owners/{otherOwnerId}");
        var crossClinicPatient = await SendAuthenticatedAsync(demoAuth.AccessToken, HttpMethod.Get, $"/api/patients/{otherPatientId}");

        Assert.Equal(HttpStatusCode.NotFound, crossClinicOwner.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossClinicPatient.StatusCode);
    }

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private async Task<Guid> CreateOwnerAsync(string accessToken, string fullName, string phone)
    {
        var response = await PostAsAuthenticatedJsonAsync(accessToken, "/api/owners", new
        {
            fullName,
            identificationNumber = (string?)null,
            phone,
            email = $"{Guid.NewGuid():N}@owner.test",
            address = "San Jose",
            alternateContact = (string?)null,
            consentNotes = "Consentimiento pendiente de digitalizar",
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
            breed = "Mestizo",
            birthDate = (DateOnly?)null,
            estimatedAge = "3 anos",
            sex = "Hembra",
            reproductiveStatus = "Esterilizada",
            color = "Cafe",
            currentWeightKg = 12.4m,
            microchipNumber = (string?)null,
            photoUrl = (string?)null,
            allergies = "Sin alergias conocidas",
            chronicDiseases = (string?)null,
            currentMedications = (string?)null,
            vaccinationStatus = "Al dia",
            dewormingStatus = "Pendiente",
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

    private async Task<HttpResponseMessage> PostAsAuthenticatedJsonAsync<TBody>(
        string accessToken,
        string requestUri,
        TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(string accessToken, HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }
}
