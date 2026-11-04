using OpenTelemetry.Exporter;

namespace euSindico.Api.Observability;

/// <summary>
/// Configuração do exportador OTLP para um backend HTTP como o Grafana Cloud (ver GETTING_STARTED.md,
/// seção "Configurando o OpenTelemetry").
/// </summary>
public static class OtlpExporterConfiguration
{
    /// <summary>
    /// Aplica protocolo, endpoint e headers. Quando o endpoint é definido por código (e não pela variável
    /// OTEL_EXPORTER_OTLP_ENDPOINT), o exportador HTTP não acrescenta o caminho do sinal sozinho — por
    /// isso "/v1/{sinal}" é montado aqui a partir do endpoint base (ex: "https://otlp-gateway-.../otlp").
    /// </summary>
    public static void Apply(OtlpExporterOptions options, string endpointBase, string? headers, string signal)
    {
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = new Uri($"{endpointBase.TrimEnd('/')}/v1/{signal}");

        if (!string.IsNullOrWhiteSpace(headers))
        {
            options.Headers = headers;
        }
    }
}
