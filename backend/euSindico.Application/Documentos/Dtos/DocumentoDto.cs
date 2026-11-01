namespace euSindico.Application.Documentos.Dtos;

public record DocumentoDto(
    int Id,
    string NomeArquivo,
    int PredioId,
    string PredioNome,
    DateTime CriadoEm);
