namespace euSindico.Application.Compromissos;

/// <summary>
/// Critérios já resolvidos pelo <see cref="CompromissoService"/> para o repositório consultar.
/// <paramref name="Hoje"/>/<paramref name="Agora"/> vêm do relógio configurado (fuso da aplicação) —
/// o repositório não conhece fuso nem relógio. <paramref name="Ano"/>/<paramref name="Mes"/> restringem pela
/// data do compromisso: valem juntos (aquele mês daquele ano) ou nenhum dos dois.
/// </summary>
public record CompromissoFiltro(
    int UsuarioId,
    string? Titulo,
    int? PredioId,
    EscopoCompromissos Escopo,
    SituacaoCompromissos Situacao,
    DateOnly Hoje,
    TimeOnly Agora,
    int? Ano = null,
    int? Mes = null);
