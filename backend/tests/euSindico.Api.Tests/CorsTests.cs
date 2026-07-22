using Microsoft.AspNetCore.Mvc.Testing;

namespace euSindico.Api.Tests;

public class CorsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Origem configurada em appsettings.Development.json ("Cors:AllowedOrigins") — ambiente
    // usado por padrão pelo WebApplicationFactory nos testes.
    private const string OrigemPermitida = "http://localhost:5173";
    private const string OrigemNaoCadastrada = "http://origem-nao-cadastrada.com";

    [Fact]
    public async Task Requisicao_de_origem_permitida_recebe_header_access_control_allow_origin()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("Origin", OrigemPermitida);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores));
        Assert.Equal(OrigemPermitida, valores.Single());

        // Necessário para o cookie HttpOnly do refresh token trafegar entre origens
        // diferentes (ver SECURITY.md, seção 1 e seção 8 "Comunicação").
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credenciais));
        Assert.Equal("true", credenciais.Single());
    }

    [Fact]
    public async Task Requisicao_de_origem_nao_cadastrada_nao_recebe_header_cors()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("Origin", OrigemNaoCadastrada);

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
