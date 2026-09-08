namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o usuário tem vínculo com o prédio (ver <see cref="PredioNaoEncontradoException"/>),
/// mas o papel desse vínculo não permite a ação solicitada (RF33) — diferente de "não existe",
/// aqui não há risco de enumeração: o usuário já sabe que o prédio existe, é membro dele.
/// </summary>
public class AcaoNaoPermitidaException() : Exception("Seu perfil de acesso não permite esta ação.");
