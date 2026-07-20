using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default);
    Task AdicionarAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task AtualizarAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}
