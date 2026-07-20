using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task AdicionarAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(ct);
    }

    public async Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        await context.RefreshTokens
            .Where(rt => rt.UsuarioId == usuarioId && rt.RevogadoEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevogadoEm, agora), ct);
    }
}
