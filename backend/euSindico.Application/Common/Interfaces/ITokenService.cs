using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface ITokenService
{
    string GerarAccessToken(Usuario usuario);
    RefreshTokenGerado GerarRefreshToken();
}

/// <summary>
/// Par gerado numa renovação: <paramref name="Token"/> vai para o cliente,
/// <paramref name="Hash"/> é o que fica persistido em <see cref="RefreshToken"/>.
/// </summary>
public record RefreshTokenGerado(string Token, string Hash);
