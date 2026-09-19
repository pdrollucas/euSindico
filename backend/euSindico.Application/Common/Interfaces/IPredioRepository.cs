using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IPredioRepository
{
    Task<int> ContarAtivosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>
    /// <paramref name="excluirId"/> exclui um prédio da comparação (o próprio prédio sendo
    /// editado, Fluxo 4 de PREDIOS.md) — sem isso, salvar um prédio sem mudar nome/endereço
    /// sempre daria falso-positivo de duplicidade contra si mesmo.
    /// </summary>
    Task<bool> ExisteNomeEEnderecoAtivoAsync(int usuarioId, string nome, string endereco, int? excluirId = null, CancellationToken ct = default);

    /// <summary>
    /// Busca o prédio pelo Id, incluindo os excluídos — quem chama decide o que fazer com
    /// <see cref="Predio.Excluido"/> (ex: <see cref="Predios.PredioService"/> trata como
    /// "não encontrado", RN08). Retorna <c>null</c> se o Id não existir.
    /// </summary>
    Task<Predio?> BuscarPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Persiste o prédio e, na mesma transação, a linha de dono em <c>predio_usuarios</c>
    /// (<see cref="PredioUsuario.CriarComoDono"/>) — sem isso, o próprio criador não passaria
    /// na checagem do <see cref="Equipe.AutorizacaoPredioService"/> sobre o prédio recém-criado.
    /// Ver PREDIOS.md, Fluxo 1.
    /// </summary>
    Task<Predio> AdicionarComDonoAsync(Predio predio, CancellationToken ct = default);

    /// <summary>
    /// Persiste alterações num <see cref="Predio"/> já rastreado pelo <c>DbContext</c> (buscado
    /// antes via <see cref="BuscarPorIdAsync"/> na mesma requisição) — mesmo padrão de
    /// <c>UsuarioRepository.AtualizarAsync</c>: só falta chamar <c>SaveChangesAsync</c>.
    /// </summary>
    Task AtualizarAsync(Predio predio, CancellationToken ct = default);
}
