using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using euSindico.Api;
using euSindico.Api.Controllers;
using euSindico.Api.Middleware;
using euSindico.Api.Observability;
using euSindico.Api.OpenApi;
using euSindico.Application;
using euSindico.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Origem(ns) do frontend autorizadas a chamar a API vindas de "Cors:AllowedOrigins" — sem
// nenhuma origem configurada, a policy não libera nenhuma (fail-closed), em vez de usar
// AllowAnyOrigin. Ver SECURITY.md, seção 8/12, e o SECURITY.md do frontend, seção 9.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Necessário para o cookie HttpOnly do refresh token (ver SECURITY.md, seção 1)
            // trafegar entre origens diferentes — exige origens explícitas, incompatível
            // com AllowAnyOrigin (já não usado, ver comentário acima).
            .AllowCredentials();
    });
});

// IP real do cliente atrás do proxy reverso (Caddy em produção), usado pelo rate limiting por IP — ver
// SECURITY.md, seção 9. Só confia em X-Forwarded-For/X-Forwarded-Proto vindos das redes listadas em
// "ReverseProxy:KnownNetworks" (CIDR, ex: a rede Docker do proxy). Sem nenhuma rede configurada, o
// middleware nem é registrado (fail-closed): um cliente que chegue direto na API não consegue forjar o IP.
var proxyKnownNetworks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [];

if (proxyKnownNetworks.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Um único proxy (o Caddy) na frente: só o último valor do X-Forwarded-For é considerado.
        options.ForwardLimit = 1;
        // Os padrões confiam em loopback, que não é o proxy aqui — a confiança fica só nas redes configuradas.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var rede in proxyKnownNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(rede));
        }
    });
}

builder.Services.AddHealthChecks();

// Falha já na inicialização se a chave não estiver configurada, em vez de deixar o erro
// estourar (de forma bem menos óbvia) só na primeira requisição que passar pelo
// middleware de autenticação — ver SECURITY.md.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException(
        "Configuração ausente: \"Jwt:SecretKey\". Configure via User Secrets em desenvolvimento " +
        "(dotnet user-secrets set \"Jwt:SecretKey\" \"...\", ver GETTING_STARTED.md) ou variável de " +
        "ambiente Jwt__SecretKey em outros ambientes (CI, produção).");
}

// Validação do access token JWT (stateless, sem consulta ao banco). A geração do token
// vive na Infrastructure (TokenService) — aqui só configuramos como validar o que chega.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // mantém os nomes de claim originais (sub, email), sem remapear para URIs legadas
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// Limita tentativas em /auth/login e /auth/registrar (força bruta / cadastro automatizado) —
// ver SECURITY.md, seção 10. Partição por IP + rota, sem fila: rejeita na hora (429) em vez
// de enfileirar, que só atrasaria o ataque em vez de bloqueá-lo.
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas tentativas. Tente novamente em instantes.",
            },
            ct);
    };

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Request.Path}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    // Envio de atas/normas (ver DOCUMENTOS.md): rajada de uploads é cara (até 20 MB cada, em memória e S3).
    // Partição por usuário (claim sub), não por IP — independe do ForwardedHeaders do proxy reverso e não
    // pune quem divide o mesmo IP. O UseRateLimiter roda depois da autenticação, então o sub já existe;
    // o IP é só reserva. 10 por minuto, sem fila.
    options.AddPolicy(DocumentoControllerBase.PoliticaRateLimitEnvio, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonimo",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    // Mensagens ao suporte (cada uma vira um e-mail na caixa da equipe): 3 por hora por usuário, mesma
    // partição por claim sub do envio de documentos. Sem fila.
    options.AddPolicy(SuporteController.PoliticaRateLimitEnvio, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonimo",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
        }));
});

// Observabilidade via OpenTelemetry (traces, métricas e logs do ILogger), exportando por OTLP/HTTP para
// o backend configurado (ex: Grafana Cloud). Fica desativada quando "Observability:OtlpEndpoint" não é
// definido (padrão em desenvolvimento local).
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];
var otlpHeaders = builder.Configuration["Observability:OtlpHeaders"];

if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("euSindico.Api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(options => OtlpExporterConfiguration.Apply(options, otlpEndpoint, otlpHeaders, "traces")))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options => OtlpExporterConfiguration.Apply(options, otlpEndpoint, otlpHeaders, "metrics")))
        .WithLogging(logging => logging
            .AddOtlpExporter(options => OtlpExporterConfiguration.Apply(options, otlpEndpoint, otlpHeaders, "logs")));
}

var app = builder.Build();

if (MigrationCommand.IsRequested(args))
{
    await MigrationCommand.RunAsync(app.Services);
    return;
}

// Configure the HTTP request pipeline.

// Primeiro do pipeline: tudo depois dele (rate limiting, logs, redirecionamento HTTPS) já vê o IP e o
// esquema reais do cliente.
if (proxyKnownNetworks.Length > 0)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Liveness: só indica que o processo está de pé e respondendo, sem consultar o banco — usado no
// healthcheck do container e na checagem rápida antes da apresentação.
app.MapHealthChecks("/health");

app.MapControllers();

await app.RunAsync();
