using euSindico.Application.Common.Interfaces;
using euSindico.Infrastructure.Persistence;
using euSindico.Infrastructure.Repositories;
using euSindico.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        // Versão fixa em vez de ServerVersion.AutoDetect: evita exigir uma conexão real
        // com o MySQL no momento em que o DbContext é configurado (ex: durante testes de
        // integração ou no início da aplicação sem banco disponível).
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
