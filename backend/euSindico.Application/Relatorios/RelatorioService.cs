using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Relatorios.Dtos;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace euSindico.Application.Relatorios;

public class RelatorioService(
    IRelatorioRepository relatorioRepository,
    ICompromissoRepository compromissoRepository,
    IFileStorageService fileStorage,
    IRelatorioPdfGenerator pdfGenerator,
    AutorizacaoPredioService autorizacaoPredioService,
    TimeProvider timeProvider,
    ILogger<RelatorioService> logger)
{
    public const int LimiteGeracoesPorMes = 3;

    private const string ContentTypePdf = "application/pdf";

    // RF26, RN06, RN13, RN33–RN35 — RELATORIOS.md, Fluxo 1. Cada geração cria um novo registro/arquivo.
    public async Task<RelatorioDto> GerarAsync(
        int usuarioId, int predioId, int mes, int ano, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.GerenciarRelatorios, ct);

        var agora = timeProvider.GetLocalNow().DateTime;
        if (ano > agora.Year || (ano == agora.Year && mes > agora.Month))
        {
            throw new PeriodoRelatorioFuturoException();
        }

        // RN36: o limite conta os relatórios existentes do mês de referência; excluir libera vaga.
        if (await relatorioRepository.ContarAsync(predioId, ano, mes, ct) >= LimiteGeracoesPorMes)
        {
            throw new LimiteRelatoriosAtingidoException(LimiteGeracoesPorMes);
        }

        var compromissos = await compromissoRepository.ListarConcluidosDoMesAsync(predioId, ano, mes, ct);
        if (compromissos.Count == 0)
        {
            throw new RelatorioSemCompromissosException();
        }

        var dados = new DadosRelatorio(
            predio.Nome,
            predio.Endereco,
            mes,
            ano,
            agora,
            compromissos
                .Select(c => new ItemRelatorio(
                    c.DataCompromisso,
                    c.HorarioCompromisso,
                    c.Titulo,
                    c.NomePrestadorServico,
                    c.Detalhes,
                    c.Responsavel?.Nome ?? string.Empty,
                    c.Orcamento))
                .ToList());

        var pdf = pdfGenerator.Gerar(dados);

        var chave = ChaveRelatorio.Nova(predioId);
        await fileStorage.SalvarAsync(chave, pdf, ContentTypePdf, ct);

        var relatorio = new Relatorio(mes, ano, NomeArquivoRelatorio.Montar(predio.Nome, mes, ano), chave, predioId);

        try
        {
            await relatorioRepository.AdicionarAsync(relatorio, ct);
        }
        catch
        {
            // Sem o registro o arquivo ficaria órfão e inalcançável: desfaz o upload.
            await fileStorage.RemoverAsync(chave, CancellationToken.None);
            throw;
        }

        return ParaDto(relatorio, predio.Nome);
    }

    // RF27, RNF10 — RELATORIOS.md, Fluxo 2. Todos os papéis vinculados listam.
    public async Task<PagedResultDto<RelatorioDto>> ListarAsync(
        int usuarioId, int predioId, int? ano, int? mes, int page, int pageSize, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarRelatorios, ct);

        var (itens, total) = await relatorioRepository.ListarAsync(predioId, ano, mes, page, pageSize, ct);

        return new PagedResultDto<RelatorioDto>(
            itens.Select(r => ParaDto(r, predio.Nome)).ToList(), page, pageSize, total);
    }

    // RF27 — RELATORIOS.md, Fluxo 3.
    public async Task<RelatorioDto> ObterAsync(
        int usuarioId, int predioId, int relatorioId, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarRelatorios, ct);
        var relatorio = await CarregarAsync(predioId, relatorioId, ct);

        return ParaDto(relatorio, predio.Nome);
    }

    // RF28 — RELATORIOS.md, Fluxo 4. O arquivo passa pela API: o bucket é privado (RFC 6.1).
    public async Task<RelatorioArquivoDto> BaixarAsync(
        int usuarioId, int predioId, int relatorioId, CancellationToken ct = default)
    {
        await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarRelatorios, ct);
        var relatorio = await CarregarAsync(predioId, relatorioId, ct);

        var conteudo = await fileStorage.LerAsync(relatorio.UrlArquivo, ct)
            ?? throw new RelatorioNaoEncontradoException();

        return new RelatorioArquivoDto(relatorio.NomeArquivo, conteudo);
    }

    // RN36 — RELATORIOS.md, Fluxo 5. Exclusão física do registro e do arquivo.
    public async Task ExcluirAsync(int usuarioId, int predioId, int relatorioId, CancellationToken ct = default)
    {
        await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.ExcluirRelatorios, ct);
        var relatorio = await CarregarAsync(predioId, relatorioId, ct);

        await relatorioRepository.RemoverAsync(relatorio, ct);

        // O registro já se foi: falha ao apagar o arquivo não vira erro para quem excluiu,
        // só um arquivo órfão a limpar (a chave fica no log).
        try
        {
            await fileStorage.RemoverAsync(relatorio.UrlArquivo, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Arquivo de relatório órfão no armazenamento: {Chave}", relatorio.UrlArquivo);
        }
    }

    // Vínculo + papel permitido (RN16, RF33) e prédio não excluído (RN08, RN09).
    private async Task<Predio> AcessarPredioAsync(int usuarioId, int predioId, AcaoPredio acao, CancellationToken ct)
    {
        var vinculo = await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, acao, ct);

        if (vinculo.Predio is null || vinculo.Predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        return vinculo.Predio;
    }

    // Relatório de outro prédio é tratado como inexistente (mesma razão de PlanejamentoService).
    private async Task<Relatorio> CarregarAsync(int predioId, int relatorioId, CancellationToken ct)
    {
        var relatorio = await relatorioRepository.BuscarPorIdAsync(relatorioId, ct);

        if (relatorio is null || relatorio.PredioId != predioId)
        {
            throw new RelatorioNaoEncontradoException();
        }

        return relatorio;
    }

    private static RelatorioDto ParaDto(Relatorio r, string predioNome) =>
        new(r.Id, r.MesReferencia, r.AnoReferencia, r.NomeArquivo, r.PredioId, predioNome, r.GeradoEm);
}
