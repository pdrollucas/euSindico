namespace euSindico.Domain.Enums;

/// <summary>
/// Tipos de documento de um prédio (RF22–RF25). Os números são os ids semeados em
/// <c>tipo_documento</c> (<c>TipoDocumentoConfiguration</c>) e ficam persistidos em
/// <c>documentos.tipo_documento_id</c> — nunca renumerar, só acrescentar.
/// </summary>
public enum TipoDocumentoPredio
{
    Ata = 1,
    Norma = 2,
}
