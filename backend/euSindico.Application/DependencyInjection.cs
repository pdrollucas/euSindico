using euSindico.Application.Auth;
using euSindico.Application.Compromissos;
using euSindico.Application.Equipe;
using euSindico.Application.Planejamentos;
using euSindico.Application.Predios;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PerfilService>();
        services.AddScoped<AutorizacaoPredioService>();
        services.AddScoped<EquipeService>();
        services.AddScoped<PredioService>();
        services.AddScoped<CompromissoService>();
        services.AddScoped<PlanejamentoService>();

        return services;
    }
}
