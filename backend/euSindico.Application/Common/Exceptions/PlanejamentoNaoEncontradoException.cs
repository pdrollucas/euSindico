namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o planejamento não existe no prédio da rota. Quando o usuário não tem vínculo
/// com o prédio (ou ele foi excluído), o erro é <see cref="PredioNaoEncontradoException"/> —
/// ambos viram 404, sem distinguir as causas (anti-enumeração).
/// </summary>
public class PlanejamentoNaoEncontradoException() : Exception("Planejamento não encontrado.");
