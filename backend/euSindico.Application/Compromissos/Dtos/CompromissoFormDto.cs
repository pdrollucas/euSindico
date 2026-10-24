namespace euSindico.Application.Compromissos.Dtos;

// Um DTO só para criar e editar (mesmo raciocínio de PredioFormDto). Prédio, data e horário são
// anuláveis para o validador distinguir "não enviado" de um valor real (TimeOnly sem valor viraria
// 00:00 silenciosamente, RN10) e para um `null` explícito não falhar na desserialização, antes do
// validador, com o formato de erro padrão do ASP.NET em vez do do projeto.
public record CompromissoFormDto(
    int? PredioId,
    string Titulo,
    DateOnly? DataCompromisso,
    TimeOnly? HorarioCompromisso,
    string? NomePrestadorServico,
    string? Detalhes,
    int? ResponsavelUsuarioId,
    decimal? Orcamento = null);
