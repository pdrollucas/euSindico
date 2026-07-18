using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace euSindico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Versão fixa em vez de ServerVersion.AutoDetect: evita exigir uma conexão real
        // com o MySQL no momento em que o DbContext é configurado (ex: durante testes de
        // integração ou no início da aplicação sem banco disponível).
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

        return services;
    }
}
