namespace euSindico.Application.Predios.Dtos;

// Usado tanto na criação (POST) quanto na edição (PUT) — os dois têm exatamente o mesmo
// formato (Nome, Endereco) e as mesmas regras, então um único DTO/validator basta.
public record PredioFormDto(string Nome, string Endereco);
