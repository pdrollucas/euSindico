namespace euSindico.Application.Compromissos.Dtos;

// Anulável para o validador rejeitar um corpo sem o campo (senão {} reabriria o compromisso).
public record AlterarConclusaoDto(bool? Concluido);
