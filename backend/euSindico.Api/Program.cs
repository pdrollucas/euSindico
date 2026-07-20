using System.Text;
using euSindico.Api.Middleware;
using euSindico.Api.OpenApi;
using euSindico.Application;
using euSindico.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? string.Empty)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

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

app.MapControllers();

app.Run();

public partial class Program { }
