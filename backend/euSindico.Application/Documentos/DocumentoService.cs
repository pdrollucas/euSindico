using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Documentos.Dtos;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace euSindico.Application.Documentos;

/// <summary>
/// Casos de uso de atas e normas (RF22–RF25). Um único service para os dois tipos: o tipo vem da rota
/// (telas separadas no front) e um documento só é alcançado pelo tipo com que foi enviado. DOCUMENTOS.md.
/// </summary>
public class DocumentoService(
    IDocumentoRepository documentoRepository,
    IFileStorageService fileStorage,
    AutorizacaoPredioService autorizacaoPredioService,
    TimeProvider timeProvider,
    ILogger<DocumentoService> logger)
{
    public const int LimiteNormasPorPredio = 10;
    public const int LimiteAtasPorMes = 5;

    // RF22, RN05, RN12 — DOCUMENTOS.md, Fluxo 1.
    public async Task<DocumentoDto> EnviarAsync(
        int usuarioId,
        int predioId,
        TipoDocumentoPredio tipo,
        string? nomeArquivo,
        byte[] conteudo,
        CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.GerenciarDocumentos, ct);

        var arquivo = ArquivoDocumento.Validar(nomeArquivo, conteudo);

        await VerificarLimiteAsync(predioId, tipo, ct);

        var chave = ChaveDocumento.Nova(predioId, arquivo.Extensao);
        await fileStorage.SalvarAsync(chave, conteudo, arquivo.ContentType, ct);

        var documento = new Documento(arquivo.NomeArquivo, chave, (int)tipo, predioId);

        try
        {
            await documentoRepository.AdicionarAsync(documento, ct);
        }
        catch
        {
            // Sem o registro o arquivo ficaria órfão e inalcançável: desfaz o upload.
            await fileStorage.RemoverAsync(chave, CancellationToken.None);
            throw;
        }

        return ParaDto(documento, predio.Nome);
    }

    // RF23, RNF10 — DOCUMENTOS.md, Fluxo 2. Todos os papéis vinculados listam.
    public async Task<PagedResultDto<DocumentoDto>> ListarAsync(
        int usuarioId, int predioId, TipoDocumentoPredio tipo, string? nome, int page, int pageSize, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarDocumentos, ct);

        var nomeFiltro = string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
        var (itens, total) = await documentoRepository.ListarAsync(predioId, tipo, nomeFiltro, page, pageSize, ct);

        return new PagedResultDto<DocumentoDto>(
            itens.Select(d => ParaDto(d, predio.Nome)).ToList(), page, pageSize, total);
    }

    // RF23 — DOCUMENTOS.md, Fluxo 3.
    public async Task<DocumentoDto> ObterAsync(
        int usuarioId, int predioId, TipoDocumentoPredio tipo, int documentoId, CancellationToken ct = default)
    {
        var predio = await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarDocumentos, ct);
        var documento = await CarregarAsync(predioId, tipo, documentoId, ct);

        return ParaDto(documento, predio.Nome);
    }

    // RF24 — DOCUMENTOS.md, Fluxo 4. O arquivo passa pela API: o bucket é privado (RFC 6.1).
    public async Task<DocumentoArquivoDto> BaixarAsync(
        int usuarioId, int predioId, TipoDocumentoPredio tipo, int documentoId, CancellationToken ct = default)
    {
        await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.VisualizarDocumentos, ct);
        var documento = await CarregarAsync(predioId, tipo, documentoId, ct);

        var conteudo = await fileStorage.LerAsync(documento.UrlArquivo, ct)
            ?? throw new DocumentoNaoEncontradoException();

        return new DocumentoArquivoDto(
            documento.NomeArquivo, ArquivoDocumento.ContentTypeDe(documento.NomeArquivo), conteudo);
    }

    // RF25 — DOCUMENTOS.md, Fluxo 5. Exclusão física do registro e do arquivo.
    public async Task ExcluirAsync(
        int usuarioId, int predioId, TipoDocumentoPredio tipo, int documentoId, CancellationToken ct = default)
    {
        await AcessarPredioAsync(usuarioId, predioId, AcaoPredio.ExcluirDocumentos, ct);
        var documento = await CarregarAsync(predioId, tipo, documentoId, ct);

        await documentoRepository.RemoverAsync(documento, ct);

        // O registro já se foi: falha ao apagar o arquivo não vira erro para quem excluiu,
        // só um arquivo órfão a limpar (a chave fica no log).
        try
        {
            await fileStorage.RemoverAsync(documento.UrlArquivo, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Arquivo de documento órfão no armazenamento: {Chave}", documento.UrlArquivo);
        }
    }

    // RN40/RN41: os limites contam os documentos existentes, então excluir libera vaga. Contar e
    // inserir não são atômicos: dois envios simultâneos podem passar do limite em um arquivo (aceito).
    private async Task VerificarLimiteAsync(int predioId, TipoDocumentoPredio tipo, CancellationToken ct)
    {
        if (tipo == TipoDocumentoPredio.Norma)
        {
            if (await documentoRepository.ContarAsync(predioId, tipo, ct: ct) >= LimiteNormasPorPredio)
            {
                throw new LimiteNormasAtingidoException(LimiteNormasPorPredio);
            }

            return;
        }

        // O mês é o do fuso da aplicação, não o do servidor; CriadoEm é gravado em UTC.
        var fuso = timeProvider.LocalTimeZone;
        var agora = timeProvider.GetLocalNow();
        var inicioDoMes = new DateTime(agora.Year, agora.Month, 1);
        var inicioDoProximoMes = inicioDoMes.AddMonths(1);

        var deUtc = new DateTimeOffset(inicioDoMes, fuso.GetUtcOffset(inicioDoMes)).UtcDateTime;
        var ateUtc = new DateTimeOffset(inicioDoProximoMes, fuso.GetUtcOffset(inicioDoProximoMes)).UtcDateTime;

        if (await documentoRepository.ContarAsync(predioId, tipo, deUtc, ateUtc, ct) >= LimiteAtasPorMes)
        {
            throw new LimiteAtasMensalAtingidoException(LimiteAtasPorMes);
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

    // Documento de outro prédio ou de outro tipo (uma ata pela rota de normas) é tratado como inexistente.
    private async Task<Documento> CarregarAsync(int predioId, TipoDocumentoPredio tipo, int documentoId, CancellationToken ct)
    {
        var documento = await documentoRepository.BuscarPorIdAsync(documentoId, ct);

        if (documento is null || documento.PredioId != predioId || documento.TipoDocumentoId != (int)tipo)
        {
            throw new DocumentoNaoEncontradoException();
        }

        return documento;
    }

    private static DocumentoDto ParaDto(Documento d, string predioNome) =>
        new(d.Id, d.NomeArquivo, d.PredioId, predioNome, d.CriadoEm);
}
