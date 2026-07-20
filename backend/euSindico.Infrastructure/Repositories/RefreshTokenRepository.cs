using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task AdicionarAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(ct);
    }

    // O parâmetro já veio de BuscarPorHashAsync no mesmo DbContext (escopo por requisição),
    // então já está tracked — só falta persistir, igual ao UsuarioRepository.AtualizarAsync.
    public async Task AtualizarAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        await context.RefreshTokens
            .Where(rt => rt.UsuarioId == usuarioId && rt.RevogadoEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevogadoEm, agora), ct);
    }
}
