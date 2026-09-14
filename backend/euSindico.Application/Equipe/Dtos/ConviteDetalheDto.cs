using euSindico.Domain.Enums;

namespace euSindico.Application.Equipe.Dtos;

/// <summary>
/// Retornado por <c>GET /convites/{token}</c>, antes do aceite — <see cref="ContaJaExiste"/>
/// permite ao frontend decidir se mostra o formulário de nome/senha ou só um botão de
/// vincular (ver EQUIPE.md, Fluxo 2).
/// </summary>
public record ConviteDetalheDto(string PredioNome, PapelPredio Papel, bool ContaJaExiste, DateTime ExpiraEm);
