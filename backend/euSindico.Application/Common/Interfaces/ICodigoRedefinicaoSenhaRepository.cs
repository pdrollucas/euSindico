using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface ICodigoRedefinicaoSenhaRepository
{
    /// <summary>
    /// Último código gerado para o usuário, independente de já usado, expirado ou ainda
    /// válido — usado para aplicar o cooldown de 5 minutos entre solicitações (RN15).
    /// </summary>
    Task<CodigoRedefinicaoSenha?> BuscarUltimoDoUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<CodigoRedefinicaoSenha?> BuscarPorUsuarioIdEHashAsync(int usuarioId, string codigoHash, CancellationToken ct = default);
    Task AdicionarAsync(CodigoRedefinicaoSenha codigo, CancellationToken ct = default);
    Task AtualizarAsync(CodigoRedefinicaoSenha codigo, CancellationToken ct = default);

    /// <summary>
    /// Marca como usado (ver <see cref="Domain.Entities.CodigoRedefinicaoSenha.MarcarComoUsado"/>) qualquer
    /// código ainda válido do usuário — no máximo um código ativo por vez.
    /// </summary>
    Task InvalidarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}