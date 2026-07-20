using euSindico.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PerfilService>();

        return services;
    }
}
