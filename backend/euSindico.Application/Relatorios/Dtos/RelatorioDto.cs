namespace euSindico.Application.Relatorios.Dtos;

public record RelatorioDto(
    int Id,
    int MesReferencia,
    int AnoReferencia,
    string NomeArquivo,
    int PredioId,
    string PredioNome,
    DateTime GeradoEm);
