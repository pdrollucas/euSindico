using System.Net;
using System.Net.Http.Json;
using euSindico.Application.Auth.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Api.Tests;

public class ForwardedHeadersTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Rede do proxy reverso (no deploy, a rede Docker do Caddy) e o IP com que ele chega na API.
    private const string RedeDoProxy = "10.10.0.0/24";
    private static readonly IPAddress IpDoProxy = IPAddress.Parse("10.10.0.2");

    // Corpo inválido de propósito, como em RateLimitingTests: barrado pelo FluentValidation antes de
    // qualquer acesso a banco; só importa quantas requisições o rate limiter deixa passar.
    private static readonly LoginDto CorpoInvalido = new(string.Empty, string.Empty);

    [Fact]
    public async Task Com_proxy_configurado_o_rate_limit_de_login_e_por_ip_real_do_cliente()
    {
        var client = AtrasDoProxy(redeConfigurada: RedeDoProxy).CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(client, ipDoCliente: "203.0.113.10");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, ipDoCliente: "203.0.113.10")).StatusCode);

        // Outro cliente atrás do mesmo proxy tem o próprio contador — sem o ForwardedHeaders, todos
        // dividiriam o contador do IP do proxy.
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await LoginAsync(client, ipDoCliente: "203.0.113.20")).StatusCode);
    }

    [Fact]
    public async Task Sem_proxy_configurado_o_x_forwarded_for_e_ignorado()
    {
        var client = AtrasDoProxy(redeConfigurada: null).CreateClient();

        // Um IP diferente a cada tentativa: se o cabeçalho fosse aceito, o atacante escaparia do limite.
        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(client, ipDoCliente: $"198.51.100.{i + 1}");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, ipDoCliente: "198.51.100.99")).StatusCode);
    }

    [Fact]
    public async Task X_forwarded_for_de_origem_fora_da_rede_do_proxy_e_ignorado()
    {
        // Rede configurada não inclui o IP de quem conecta (10.10.0.2): o cabeçalho não é confiável.
        var client = AtrasDoProxy(redeConfigurada: "192.168.50.0/24").CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(client, ipDoCliente: $"198.51.100.{i + 1}");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, ipDoCliente: "198.51.100.99")).StatusCode);
    }

    private WebApplicationFactory<Program> AtrasDoProxy(string? redeConfigurada) =>
        factory.WithWebHostBuilder(builder =>
        {
            if (redeConfigurada is not null)
            {
                builder.UseSetting("ReverseProxy:KnownNetworks:0", redeConfigurada);
            }

            // O TestServer não define IP de conexão; simulamos que toda requisição chega pelo proxy.
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ConexaoPeloProxyStartupFilter(IpDoProxy)));
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string ipDoCliente)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login") { Content = JsonContent.Create(CorpoInvalido) };
        request.Headers.Add("X-Forwarded-For", ipDoCliente);
        return client.SendAsync(request);
    }

    private sealed class ConexaoPeloProxyStartupFilter(IPAddress ipDoProxy) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, proximo) =>
            {
                context.Connection.RemoteIpAddress = ipDoProxy;
                return proximo(context);
            });
            next(app);
        };
    }
}
