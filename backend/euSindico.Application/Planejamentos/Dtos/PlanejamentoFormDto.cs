namespace euSindico.Application.Planejamentos.Dtos;

// Um DTO só para criar e editar (mesmo raciocínio de CompromissoFormDto). O prédio vem da rota
// (/predios/{predioId}/planejamentos), não do corpo — planejamento não muda de prédio. Data e
// orçamento são opcionais (RN11).
public record PlanejamentoFormDto(
    string Titulo,
    DateOnly? DataPlanejamento,
    decimal? OrcamentoPrevisto,
    string? NomePrestadorServico,
    string? Detalhes);
