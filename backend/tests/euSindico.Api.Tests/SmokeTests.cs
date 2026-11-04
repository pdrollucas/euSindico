using Microsoft.AspNetCore.Mvc.Testing;

namespace euSindico.Api.Tests;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApi_document_e_exposto_em_desenvolvimento()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Health_responde_healthy_sem_autenticacao()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
