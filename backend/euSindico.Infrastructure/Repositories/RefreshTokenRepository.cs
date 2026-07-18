using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;

namespace euSindico.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task AdicionarAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(ct);
    }
}
