using euSindico.Domain.Enums;

namespace euSindico.Application.Equipe.Dtos;

public record EquipeMembroDto(int UsuarioId, string Nome, string Email, PapelPredio Papel, DateTime CriadoEm);
