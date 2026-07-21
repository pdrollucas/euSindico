using System.Text;
using System.Threading.RateLimiting;
using euSindico.Api.Middleware;
using euSindico.Api.OpenApi;
using euSindico.Application;
using euSindico.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
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
});

// Observabilidade via OpenTelemetry, exportando para o backend OTLP configurado (ex: Grafana Cloud).
// Fica desativada quando "Observability:OtlpEndpoint" não é definido (padrão em desenvolvimento local).
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];
var otlpHeaders = builder.Configuration["Observability:OtlpHeaders"];

if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    void ConfigureOtlpExporter(OtlpExporterOptions options)
    {
        options.Endpoint = new Uri(otlpEndpoint);
        if (!string.IsNullOrWhiteSpace(otlpHeaders))
        {
            options.Headers = otlpHeaders;
        }
    }

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("euSindico.Api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(ConfigureOtlpExporter))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(ConfigureOtlpExporter));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }
