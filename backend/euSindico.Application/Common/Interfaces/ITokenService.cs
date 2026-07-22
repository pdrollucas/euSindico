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

    /// <summary>
    /// Gera o código de redefinição de senha (RF06-A): 6 caracteres, só A-Z/0-9, sem
    /// caracteres visualmente ambíguos (ver SECURITY.md, seção 10).
    /// </summary>
    CodigoRedefinicaoSenhaGerado GerarCodigoRedefinicaoSenha();

    /// <summary>
    /// Recalcula o hash de um código de redefinição recebido do cliente, normalizando
    /// (maiúsculas, sem espaços) antes de hashear — validação case-insensitive.
    /// </summary>
    string HashCodigoRedefinicaoSenha(string codigo);
}

/// <summary>
/// Par gerado numa renovação: <paramref name="Token"/> vai para o cliente,
/// <paramref name="Hash"/> é o que fica persistido em <see cref="RefreshToken"/>.
/// </summary>
public record RefreshTokenGerado(string Token, string Hash);

/// <summary>
/// Par gerado numa solicitação de redefinição de senha: <paramref name="Codigo"/> vai para
/// o e-mail do usuário, <paramref name="Hash"/> é o que fica persistido em <see cref="euSindico.Domain.Entities.CodigoRedefinicaoSenha"/>.
/// </summary>
public record CodigoRedefinicaoSenhaGerado(string Codigo, string Hash);
