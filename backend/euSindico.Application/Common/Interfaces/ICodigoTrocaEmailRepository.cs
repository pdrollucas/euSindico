using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface ICodigoTrocaEmailRepository
{
    /// <summary>
    /// Último código gerado para o usuário, independente de já usado, expirado ou ainda
    /// válido — usado para aplicar o cooldown entre solicitações.
    /// </summary>
    Task<CodigoTrocaEmail?> BuscarUltimoDoUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<CodigoTrocaEmail?> BuscarPorUsuarioIdEHashAsync(int usuarioId, string codigoHash, CancellationToken ct = default);
    Task AdicionarAsync(CodigoTrocaEmail codigo, CancellationToken ct = default);
    Task AtualizarAsync(CodigoTrocaEmail codigo, CancellationToken ct = default);

    /// <summary>
    /// Marca como usado qualquer código ainda válido do usuário — no máximo um pedido de troca ativo por vez.
    /// </summary>
    Task InvalidarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}
