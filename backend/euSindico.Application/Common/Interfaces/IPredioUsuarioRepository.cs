using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IPredioUsuarioRepository
{
    /// <summary>
    /// Vínculo de um usuário com um prédio, se existir — base de toda checagem de acesso
    /// (RN16), usada pelo <see cref="Equipe.AutorizacaoPredioService"/>. Inclui
    /// <see cref="PredioUsuario.Usuario"/> e <see cref="PredioUsuario.Predio"/> carregados,
    /// já que quem chama normalmente precisa desses dados logo em seguida (ex:
    /// <see cref="Equipe.EquipeService.ConvidarAsync"/> usa os dois para montar o e-mail de
    /// convite, sem uma segunda consulta).
    /// </summary>
    Task<PredioUsuario?> BuscarVinculoAsync(int predioId, int usuarioId, CancellationToken ct = default);

    Task<bool> ExisteMembroComEmailAsync(int predioId, string email, CancellationToken ct = default);

    /// <summary>
    /// Prédios vinculados a um usuário (RF09, RN16), qualquer papel — não só os criados por
    /// ele. Cada item traz <see cref="PredioUsuario.Predio"/> carregado (papel + dados do
    /// prédio numa única consulta). Filtra prédios excluídos (RN08). Ordenado por
    /// <c>Predio.Nome</c> crescente (ver PREDIOS.md, Paginação).
    /// </summary>
    Task<(IReadOnlyList<PredioUsuario> Itens, int Total)> ListarPrediosDoUsuarioAsync(
        int usuarioId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Todos os membros de um prédio (RF31), com <see cref="PredioUsuario.Usuario"/> já carregado
    /// para expor nome/e-mail sem uma segunda consulta.
    /// </summary>
    Task<List<PredioUsuario>> ListarDoPredioAsync(int predioId, CancellationToken ct = default);

    Task AdicionarAsync(PredioUsuario predioUsuario, CancellationToken ct = default);
    Task RemoverAsync(PredioUsuario predioUsuario, CancellationToken ct = default);
}
