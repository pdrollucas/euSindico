namespace euSindico.Application.Relatorios;

public record DadosRelatorio(
    string PredioNome,
    string PredioEndereco,
    int Mes,
    int Ano,
    DateTime GeradoEm,
    IReadOnlyList<ItemRelatorio> Compromissos);

public record ItemRelatorio(
    DateOnly Data,
    TimeOnly Horario,
    string Titulo,
    string? NomePrestadorServico,
    string? Detalhes,
    string ResponsavelNome);
