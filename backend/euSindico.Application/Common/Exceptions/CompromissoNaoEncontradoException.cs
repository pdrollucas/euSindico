namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o compromisso não existe. Quando ele existe mas o usuário não tem vínculo
/// com o prédio (ou o prédio foi excluído), o erro é <see cref="PredioNaoEncontradoException"/> —
/// ambos viram 404, sem distinguir as causas (anti-enumeração).
/// </summary>
public class CompromissoNaoEncontradoException() : Exception("Compromisso não encontrado.");
