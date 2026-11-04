using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Api;

/// <summary>
/// Modo "só migrations" da API: com o argumento <c>--migrate</c>, aplica as migrations pendentes e encerra,
/// sem subir o servidor HTTP. Usado pelo serviço <c>migrate</c> do docker-compose de produção, que roda
/// antes da API (mesma imagem, mesma configuração) — ver deploy/README.md.
/// </summary>
public static class MigrationCommand
{
    public const string Argument = "--migrate";

    public static bool IsRequested(string[] args) => args.Contains(Argument, StringComparer.OrdinalIgnoreCase);

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendentes = (await db.Database.GetPendingMigrationsAsync()).ToList();
        logger.LogInformation("Migrations pendentes: {Quantidade} ({Nomes})", pendentes.Count, string.Join(", ", pendentes));

        await db.Database.MigrateAsync();

        logger.LogInformation("Banco atualizado.");
    }
}
