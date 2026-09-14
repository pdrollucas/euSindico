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
