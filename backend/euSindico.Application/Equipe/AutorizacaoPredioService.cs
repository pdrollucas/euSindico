using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Equipe;

/// <summary>
/// Resolve se um usuário pode executar uma ação num prédio, a partir do seu vínculo
/// (RN16, RF33). Serviço compartilhado — todo módulo que opera sobre um prédio (Equipe hoje;
/// Compromissos, Planejamentos, Documentos e Relatórios quando existirem) chama isto em vez
/// de duplicar a checagem de posse/perfil. Ver EQUIPE.md.
/// </summary>
public class AutorizacaoPredioService(IPredioUsuarioRepository predioUsuarioRepository)
{
    // Matriz de permissões (RF33) — cresce conforme novas ações (AcaoPredio) forem definidas.
    private static readonly Dictionary<AcaoPredio, PapelPredio[]> PermissoesPorAcao = new()
    {
        [AcaoPredio.GerenciarEquipe] = [PapelPredio.Sindico],
        [AcaoPredio.VisualizarPredio] = [PapelPredio.Sindico, PapelPredio.Gestor, PapelPredio.Colaborador],
    };

    /// <summary>
    /// Confere o vínculo do usuário com o prédio (RN16) e se o papel encontrado permite a
    /// ação (RF33). Sem vínculo → <see cref="PredioNaoEncontradoException"/> (404, anti-enumeração:
    /// não distingue "não existe" de "não é seu"). Com vínculo mas sem permissão →
    /// <see cref="AcaoNaoPermitidaException"/> (403). Devolve o vínculo inteiro (não só o
    /// papel) para quem chamou poder reaproveitar <c>Usuario</c>/<c>Predio</c> já carregados
    /// (ex: <see cref="Equipe.EquipeService.ConvidarAsync"/> usa isso para o nome do síndico
    /// e do prédio no e-mail de convite, sem uma segunda consulta).
    /// </summary>
    public async Task<PredioUsuario> VerificarAcessoAsync(int usuarioId, int predioId, AcaoPredio acao, CancellationToken ct = default)
    {
        var vinculo = await predioUsuarioRepository.BuscarVinculoAsync(predioId, usuarioId, ct)
            ?? throw new PredioNaoEncontradoException();

        if (!PermissoesPorAcao[acao].Contains(vinculo.Papel))
        {
            throw new AcaoNaoPermitidaException();
        }

        return vinculo;
    }
}
