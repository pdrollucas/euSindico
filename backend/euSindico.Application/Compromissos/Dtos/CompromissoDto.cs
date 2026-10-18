namespace euSindico.Application.Compromissos.Dtos;

public record CompromissoDto(
    int Id,
    string Titulo,
    DateOnly DataCompromisso,
    TimeOnly HorarioCompromisso,
    string? NomePrestadorServico,
    string? Detalhes,
    bool Concluido,
    int PredioId,
    string PredioNome,
    int ResponsavelUsuarioId,
    string ResponsavelNome,
    DateTime CriadoEm);
