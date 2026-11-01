using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class CodigoTrocaEmailRepository(AppDbContext context) : ICodigoTrocaEmailRepository
{
    public Task<CodigoTrocaEmail?> BuscarUltimoDoUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        context.CodigosTrocaEmail
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .FirstOrDefaultAsync(ct);

    public Task<CodigoTrocaEmail?> BuscarPorUsuarioIdEHashAsync(int usuarioId, string codigoHash, CancellationToken ct = default) =>
        context.CodigosTrocaEmail.FirstOrDefaultAsync(
            c => c.UsuarioId == usuarioId && c.CodigoHash == codigoHash, ct);

    public async Task AdicionarAsync(CodigoTrocaEmail codigo, CancellationToken ct = default)
    {
        context.CodigosTrocaEmail.Add(codigo);
        await context.SaveChangesAsync(ct);
    }

    // Já tracked (veio de BuscarPorUsuarioIdEHashAsync no mesmo DbContext) — só falta persistir.
    public async Task AtualizarAsync(CodigoTrocaEmail codigo, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task InvalidarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        await context.CodigosTrocaEmail
            .Where(c => c.UsuarioId == usuarioId && c.UsadoEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.UsadoEm, agora), ct);
    }
}
