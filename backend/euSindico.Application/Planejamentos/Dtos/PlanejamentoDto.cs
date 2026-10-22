namespace euSindico.Application.Planejamentos.Dtos;

public record PlanejamentoDto(
    int Id,
    string Titulo,
    DateOnly? DataPlanejamento,
    decimal? OrcamentoPrevisto,
    string? NomePrestadorServico,
    string? Detalhes,
    int PredioId,
    string PredioNome,
    DateTime CriadoEm);
