using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface ITokenService
{
    string GerarAccessToken(Usuario usuario);
    RefreshTokenGerado GerarRefreshToken();

    /// <summary>
    /// Recalcula o hash de um refresh token recebido do cliente, para buscá-lo por
    /// <see cref="Domain.Entities.RefreshToken.TokenHash"/> — mesmo algoritmo usado em <see cref="GerarRefreshToken"/>.
    /// </summary>
    string HashRefreshToken(string refreshToken);
}

/// <summary>
/// Par gerado numa renovação: <paramref name="Token"/> vai para o cliente,
/// <paramref name="Hash"/> é o que fica persistido em <see cref="RefreshToken"/>.
/// </summary>
public record RefreshTokenGerado(string Token, string Hash);
