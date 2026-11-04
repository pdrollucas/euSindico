using euSindico.Api.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace euSindico.Api.Tests.Observability;

public class OtlpExporterConfigurationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("https://otlp-gateway-prod-sa-east-1.grafana.net/otlp", "traces", "https://otlp-gateway-prod-sa-east-1.grafana.net/otlp/v1/traces")]
    [InlineData("https://otlp-gateway-prod-sa-east-1.grafana.net/otlp/", "metrics", "https://otlp-gateway-prod-sa-east-1.grafana.net/otlp/v1/metrics")]
    [InlineData("http://localhost:4318", "logs", "http://localhost:4318/v1/logs")]
    public void Monta_o_endpoint_do_sinal_a_partir_do_endpoint_base(string endpointBase, string sinal, string esperado)
    {
        var options = new OtlpExporterOptions();

        OtlpExporterConfiguration.Apply(options, endpointBase, headers: null, sinal);

        Assert.Equal(new Uri(esperado), options.Endpoint);
    }

    [Fact]
    public void Usa_http_protobuf_que_e_o_protocolo_do_gateway_do_grafana_cloud()
    {
        var options = new OtlpExporterOptions();

        OtlpExporterConfiguration.Apply(options, "http://localhost:4318", headers: null, "traces");

        Assert.Equal(OtlpExportProtocol.HttpProtobuf, options.Protocol);
    }

    [Fact]
    public void Aplica_os_headers_de_autenticacao_quando_informados()
    {
        var options = new OtlpExporterOptions();

        OtlpExporterConfiguration.Apply(options, "http://localhost:4318", "Authorization=Basic abc123", "traces");

        Assert.Equal("Authorization=Basic abc123", options.Headers);
    }

    [Fact]
    public void Com_endpoint_configurado_os_logs_do_ilogger_sao_exportados()
    {
        using var app = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Observability:OtlpEndpoint", "http://localhost:4318"));

        var provedores = app.Services.GetServices<ILoggerProvider>();

        Assert.Contains(provedores, provedor => provedor is OpenTelemetryLoggerProvider);
    }

    [Fact]
    public void Sem_endpoint_configurado_os_logs_nao_sao_exportados()
    {
        var provedores = factory.Services.GetServices<ILoggerProvider>();

        Assert.DoesNotContain(provedores, provedor => provedor is OpenTelemetryLoggerProvider);
    }
}
