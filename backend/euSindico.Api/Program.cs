using euSindico.Api.Middleware;
using euSindico.Application;
using euSindico.Infrastructure;
using FluentValidation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

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

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
