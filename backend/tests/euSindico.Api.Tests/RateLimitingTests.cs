using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Api.Tests;

public class RateLimitingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Corpo inválido de propósito: a requisição é barrada pelo FluentValidation antes de
    // chegar ao AuthService, então o teste não depende de um banco de dados disponível —
    // só nos importa quantas requisições o rate limiter deixa passar.
    private static readonly LoginDto CorpoInvalido = new(string.Empty, string.Empty);

    [Fact]
    public async Task Login_apos_exceder_o_limite_de_tentativas_retorna_429()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var resposta = await client.PostAsJsonAsync("/auth/login", CorpoInvalido);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, resposta.StatusCode);
        }

        var respostaBloqueada = await client.PostAsJsonAsync("/auth/login", CorpoInvalido);

        Assert.Equal(HttpStatusCode.TooManyRequests, respostaBloqueada.StatusCode);
        var problema = await respostaBloqueada.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status429TooManyRequests, problema?.Status);
    }

    [Fact]
    public async Task Envio_de_documento_apos_10_requisicoes_do_mesmo_usuario_retorna_429()
    {
        var client = ClienteAutenticado(usuarioId: 501);

        // Sem arquivo de propósito: o controller responde 400 antes de qualquer acesso a banco ou S3;
        // só interessa quantas requisições o rate limiter deixa passar.
        for (var i = 0; i < 10; i++)
        {
            var resposta = await EnviarSemArquivoAsync(client, "atas");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, resposta.StatusCode);
        }

        var bloqueada = await EnviarSemArquivoAsync(client, "atas");

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
    }

    [Fact]
    public async Task Envio_de_documento_e_limitado_por_usuario_e_nao_por_rota()
    {
        var primeiro = ClienteAutenticado(usuarioId: 502);
        for (var i = 0; i < 10; i++)
        {
            await EnviarSemArquivoAsync(primeiro, "atas");
        }

        // O mesmo usuário esgotou o limite também nas normas (uma só política para os dois tipos)...
        Assert.Equal(HttpStatusCode.TooManyRequests, (await EnviarSemArquivoAsync(primeiro, "normas")).StatusCode);

        // ...mas outro usuário, no mesmo IP, não é afetado.
        var segundo = ClienteAutenticado(usuarioId: 503);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await EnviarSemArquivoAsync(segundo, "atas")).StatusCode);
    }

    private HttpClient ClienteAutenticado(int usuarioId)
    {
        var usuario = new Usuario("Teste", $"u{usuarioId}@x.com", "hash");
        typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, usuarioId);
        var token = factory.Services.GetRequiredService<ITokenService>().GerarAccessToken(usuario);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Task<HttpResponseMessage> EnviarSemArquivoAsync(HttpClient client, string tipo) =>
        client.PostAsync($"/predios/1/{tipo}", new MultipartFormDataContent());

    [Fact]
    public async Task Login_e_registrar_sao_limitados_independentemente()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/auth/login", CorpoInvalido);
        }

        // O limite de /auth/login já foi esgotado acima, mas /auth/registrar tem seu
        // próprio contador (partição por IP + rota) e ainda deve aceitar a requisição.
        var respostaRegistrar = await client.PostAsJsonAsync(
            "/auth/registrar",
            new RegistrarUsuarioDto(string.Empty, string.Empty, string.Empty));

        Assert.NotEqual(HttpStatusCode.TooManyRequests, respostaRegistrar.StatusCode);
    }
}