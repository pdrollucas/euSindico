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
}
