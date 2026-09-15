using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IPredioRepository
{
    Task<int> ContarAtivosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<bool> ExisteNomeEEnderecoAtivoAsync(int usuarioId, string nome, string endereco, CancellationToken ct = default);

    /// <summary>
    /// Persiste o prédio e, na mesma transação, a linha de dono em <c>predio_usuarios</c>
    /// (<see cref="PredioUsuario.CriarComoDono"/>) — sem isso, o próprio criador não passaria
    /// na checagem do <see cref="Equipe.AutorizacaoPredioService"/> sobre o prédio recém-criado.
    /// Ver PREDIOS.md, Fluxo 1.
    /// </summary>
    Task<Predio> AdicionarComDonoAsync(Predio predio, CancellationToken ct = default);
}
