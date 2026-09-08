namespace euSindico.Application.Equipe.Dtos;

/// <summary>
/// <see cref="Nome"/>/<see cref="Senha"/> só são obrigatórios quando o e-mail do convite
/// ainda não tem conta — se já tiver, são ignorados (ver EQUIPE.md, Fluxo 2).
/// </summary>
public record AceitarConviteDto(string? Nome, string? Senha);
