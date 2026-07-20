using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task AdicionarAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}
