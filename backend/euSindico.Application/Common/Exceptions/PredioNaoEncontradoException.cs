namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o usuário não tem nenhum vínculo (<see cref="Domain.Entities.PredioUsuario"/>)
/// com o prédio solicitado — cobre, de propósito, "não existe", "não é seu" e "acesso removido"
/// com a mesma resposta (anti-enumeração, mesmo espírito de <see cref="CredenciaisInvalidasException"/>).
/// </summary>
public class PredioNaoEncontradoException() : Exception("Prédio não encontrado.");
