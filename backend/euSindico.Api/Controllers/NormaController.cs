using euSindico.Application.Documentos;
using euSindico.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[Route("predios/{predioId:int}/normas")]
public class NormaController(DocumentoService documentoService) : DocumentoControllerBase(documentoService)
{
    protected override TipoDocumentoPredio Tipo => TipoDocumentoPredio.Norma;
}
