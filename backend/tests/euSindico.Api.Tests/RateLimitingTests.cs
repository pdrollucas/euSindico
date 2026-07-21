using System.Net;
using System.Net.Http.Json;
using euSindico.Application.Auth.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

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