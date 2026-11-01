namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o documento não existe no prédio e no tipo (ata/norma) da rota, ou o arquivo dele
/// sumiu do armazenamento. Sem vínculo com o prédio, o erro é <see cref="PredioNaoEncontradoException"/> —
/// ambos viram 404 (anti-enumeração).
/// </summary>
public class DocumentoNaoEncontradoException() : Exception("Documento não encontrado.");
