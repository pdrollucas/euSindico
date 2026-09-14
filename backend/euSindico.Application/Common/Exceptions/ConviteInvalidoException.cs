namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Mesma mensagem genérica para token inexistente, expirado ou já usado (RN17) — anti-enumeração,
/// mesmo espírito de <see cref="RefreshTokenInvalidoException"/>/<see cref="CodigoRedefinicaoInvalidoException"/>.
/// </summary>
public class ConviteInvalidoException() : Exception("Convite inválido ou expirado.");
