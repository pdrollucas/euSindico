namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o relatório não existe no prédio da rota (ou o arquivo dele sumiu do
/// armazenamento). Sem vínculo com o prédio, o erro é <see cref="PredioNaoEncontradoException"/> —
/// ambos viram 404 (anti-enumeração).
/// </summary>
public class RelatorioNaoEncontradoException() : Exception("Relatório não encontrado.");
