using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class PredioUsuarioRepository(AppDbContext context) : IPredioUsuarioRepository
{
    public Task<PredioUsuario?> BuscarVinculoAsync(int predioId, int usuarioId, CancellationToken ct = default) =>
        context.PredioUsuarios
            .Include(pu => pu.Usuario)
            .Include(pu => pu.Predio)
            .FirstOrDefaultAsync(pu => pu.PredioId == predioId && pu.UsuarioId == usuarioId, ct);

    public Task<bool> ExisteMembroComEmailAsync(int predioId, string email, CancellationToken ct = default) =>
        context.PredioUsuarios
            .AnyAsync(pu => pu.PredioId == predioId && pu.Usuario!.Email == email, ct);

    public async Task<(IReadOnlyList<PredioUsuario> Itens, int Total)> ListarPrediosDoUsuarioAsync(
        int usuarioId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.PredioUsuarios
            .Include(pu => pu.Predio)
            .Where(pu => pu.UsuarioId == usuarioId && !pu.Predio!.Excluido);

        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderBy(pu => pu.Predio!.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itens, total);
    }

    public Task<List<PredioUsuario>> ListarDoPredioAsync(int predioId, CancellationToken ct = default) =>
        context.PredioUsuarios
            .Include(pu => pu.Usuario)
            .Where(pu => pu.PredioId == predioId)
            .OrderBy(pu => pu.CriadoEm)
            .ToListAsync(ct);

    public async Task AdicionarAsync(PredioUsuario predioUsuario, CancellationToken ct = default)
    {
        context.PredioUsuarios.Add(predioUsuario);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(PredioUsuario predioUsuario, CancellationToken ct = default)
    {
        context.PredioUsuarios.Remove(predioUsuario);
        await context.SaveChangesAsync(ct);
    }
}
