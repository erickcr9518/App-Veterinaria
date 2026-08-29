using System.Net;

namespace VetPlatform.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<VetPlatformApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(VetPlatformApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Is_Reachable_Without_Authentication()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
