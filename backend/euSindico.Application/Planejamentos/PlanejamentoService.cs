using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Planejamentos.Dtos;
using euSindico.Domain.Entities;

namespace euSindico.Application.Planejamentos;

public class PlanejamentoService(
    IPlanejamentoRepository planejamentoRepository,
    AutorizacaoPredioService autorizacaoPredioService,
    TimeProvider timeProvider)
{
    // RF18, RN04, RN09, RN11 — PLANEJAMENTOS.md, Fluxo 1.
    public async Task<PlanejamentoDto> CriarAsync(
        int usuarioId, int predioId, PlanejamentoFormDto dto, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.GerenciarPlanejamentos, ct);
        ValidarData(dto.DataPlanejamento);

        var planejamento = new Planejamento(
            dto.Titulo.Trim(),
            predioId,
            dto.DataPlanejamento,
            dto.OrcamentoPrevisto,
            NormalizarOpcional(dto.NomePrestadorServico),
            NormalizarOpcional(dto.Detalhes));

        await planejamentoRepository.AdicionarAsync(planejamento, ct);

        return ParaDto(planejamento, predio.Nome);
    }

    // RF19, RNF10 — PLANEJAMENTOS.md, Fluxo 2. Todos os papéis vinculados listam (Colaborador: leitura).
    public async Task<PagedResultDto<PlanejamentoDto>> ListarAsync(
        int usuarioId,
        int predioId,
        string? titulo,
        decimal? orcamentoMinimo,
        decimal? orcamentoMaximo,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarPlanejamentos, ct);

        var filtro = new PlanejamentoFiltro(predioId, NormalizarOpcional(titulo), orcamentoMinimo, orcamentoMaximo);
        var (itens, total) = await planejamentoRepository.ListarAsync(filtro, page, pageSize, ct);

        return new PagedResultDto<PlanejamentoDto>(
            itens.Select(p => ParaDto(p, predio.Nome)).ToList(), page, pageSize, total);
    }

    // RF19 — PLANEJAMENTOS.md, Fluxo 3.
    public async Task<PlanejamentoDto> ObterAsync(
        int usuarioId, int predioId, int planejamentoId, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarPlanejamentos, ct);
        var planejamento = await CarregarAsync(predioId, planejamentoId, ct);

        return ParaDto(planejamento, predio.Nome);
    }

    // RF20, RN09, RN11 — PLANEJAMENTOS.md, Fluxo 4. Não muda de prédio.
    public async Task<PlanejamentoDto> AtualizarAsync(
        int usuarioId, int predioId, int planejamentoId, PlanejamentoFormDto dto, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.GerenciarPlanejamentos, ct);
        var planejamento = await CarregarAsync(predioId, planejamentoId, ct);
        ValidarData(dto.DataPlanejamento);

        planejamento.AtualizarDados(
            dto.Titulo.Trim(),
            dto.DataPlanejamento,
            dto.OrcamentoPrevisto,
            NormalizarOpcional(dto.NomePrestadorServico),
            NormalizarOpcional(dto.Detalhes));

        await planejamentoRepository.AtualizarAsync(planejamento, ct);

        return ParaDto(planejamento, predio.Nome);
    }

    // RF21 — exclusão física, PLANEJAMENTOS.md, Fluxo 5.
    public async Task ExcluirAsync(int usuarioId, int predioId, int planejamentoId, CancellationToken ct = default)
    {
        await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.GerenciarPlanejamentos, ct);
        var planejamento = await CarregarAsync(predioId, planejamentoId, ct);

        await planejamentoRepository.RemoverAsync(planejamento, ct);
    }

    // Vínculo + papel permitido para a ação (RN16, RF33) e prédio não excluído (RN08, RN09).
    // Devolve o prédio (o DTO de saída precisa do nome).
    private async Task<Predio> AcessarPredioAsync(int usuarioId, int predioId, AcaoPredio acao, CancellationToken ct)
    {
        var vinculo = await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, acao, ct);

        if (vinculo.Predio is null || vinculo.Predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        return vinculo.Predio;
    }

    // Planejamento de outro prédio é tratado como inexistente: o id do recurso não pode ser usado
    // para alcançar um registro de um prédio diferente do que o acesso checado acima cobre.
    private async Task<Planejamento> CarregarAsync(int predioId, int planejamentoId, CancellationToken ct)
    {
        var planejamento = await planejamentoRepository.BuscarPorIdAsync(planejamentoId, ct);

        if (planejamento is null || planejamento.PredioId != predioId)
        {
            throw new PlanejamentoNaoEncontradoException();
        }

        return planejamento;
    }

    // Data é opcional (RN11); quando informada, hoje ou futura, no fuso da aplicação — vale em toda
    // edição, mesmo que a data não tenha mudado.
    private void ValidarData(DateOnly? data)
    {
        if (data is null)
        {
            return;
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (data < hoje)
        {
            throw new DataPlanejamentoNoPassadoException();
        }
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static PlanejamentoDto ParaDto(Planejamento p, string predioNome) =>
        new(
            p.Id,
            p.Titulo,
            p.DataPlanejamento,
            p.OrcamentoPrevisto,
            p.NomePrestadorServico,
            p.Detalhes,
            p.PredioId,
            predioNome,
            p.CriadoEm);
}
