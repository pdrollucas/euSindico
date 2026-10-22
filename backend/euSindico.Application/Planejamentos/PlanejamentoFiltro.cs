namespace euSindico.Application.Planejamentos;

/// <summary>
/// Critérios já resolvidos pelo <see cref="PlanejamentoService"/> para o repositório consultar.
/// A faixa de orçamento é inclusiva nas duas pontas; planejamento sem orçamento nunca casa com ela.
/// </summary>
public record PlanejamentoFiltro(
    int PredioId,
    string? Titulo,
    decimal? OrcamentoMinimo,
    decimal? OrcamentoMaximo);
