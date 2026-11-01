using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Documentos;
using euSindico.Application.Equipe;
using euSindico.Application.Tests.Relatorios;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Application.Tests.Documentos;

public class DocumentoServiceTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;
    private const int OutroPredioId = 20;
    private const int DocumentoId = 100;

    private readonly Mock<IDocumentoRepository> _documentoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly InMemoryFileStorageService _storage = new();
    private readonly DocumentoService _sut;
    private Documento? _adicionado;

    public DocumentoServiceTests()
    {
        _documentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()))
            .Callback<Documento, CancellationToken>((d, _) => _adicionado = d)
            .Returns(Task.CompletedTask);

        _sut = new DocumentoService(
            _documentoRepository.Object,
            _storage,
            new AutorizacaoPredioService(_predioUsuarioRepository.Object),
            Relogio(new DateTimeOffset(2026, 10, 17, 1, 30, 0, TimeSpan.Zero)),
            NullLogger<DocumentoService>.Instance);
    }

    // ---------- Enviar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico, TipoDocumentoPredio.Ata)]
    [InlineData(PapelPredio.Gestor, TipoDocumentoPredio.Ata)]
    [InlineData(PapelPredio.Sindico, TipoDocumentoPredio.Norma)]
    [InlineData(PapelPredio.Gestor, TipoDocumentoPredio.Norma)]
    public async Task EnviarAsync_persiste_o_registro_com_o_tipo_e_grava_o_arquivo(PapelPredio papel, TipoDocumentoPredio tipo)
    {
        ConfigurarVinculo(papel);
        var conteudo = ArquivosDeTeste.Pdf();

        var resultado = await _sut.EnviarAsync(UsuarioId, PredioId, tipo, "Ata 03.pdf", conteudo);

        Assert.Equal("Ata 03.pdf", resultado.NomeArquivo);
        Assert.Equal(PredioId, resultado.PredioId);
        Assert.Equal("Predio 10", resultado.PredioNome);

        _documentoRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal((int)tipo, _adicionado!.TipoDocumentoId);
        Assert.Equal(PredioId, _adicionado.PredioId);

        var (chave, arquivo) = Assert.Single(_storage.Arquivos);
        Assert.Equal(chave, _adicionado.UrlArquivo);
        Assert.StartsWith($"documentos/{PredioId}/", chave);
        Assert.EndsWith(".pdf", chave);
        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal(conteudo, arquivo.Conteudo);
    }

    [Fact]
    public async Task EnviarAsync_nao_usa_o_nome_enviado_na_chave_do_armazenamento()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "Segredo do síndico.pdf", ArquivosDeTeste.Pdf());

        var chave = Assert.Single(_storage.Arquivos).Key;
        Assert.DoesNotContain("Segredo", chave);
    }

    [Fact]
    public async Task EnviarAsync_do_Colaborador_lanca_AcaoNaoPermitidaException()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "ata.pdf", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "ata.pdf", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException_RN09()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "ata.pdf", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_com_arquivo_invalido_lanca_ArquivoDocumentoInvalidoException_sem_gravar_nada()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<ArquivoDocumentoInvalidoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "virus.exe", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_confere_a_permissao_antes_de_validar_o_arquivo()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        // Arquivo inválido E sem permissão: quem não pode enviar não descobre as regras do arquivo.
        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "virus.exe", []));
    }

    [Fact]
    public async Task EnviarAsync_se_o_registro_falha_desfaz_o_upload()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        _documentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("banco fora"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "ata.pdf", ArquivosDeTeste.Pdf()));

        Assert.Empty(_storage.Arquivos);
    }

    [Fact]
    public async Task EnviarAsync_se_o_upload_falha_nao_cria_registro()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        _storage.FalharAoSalvar = true;

        await Assert.ThrowsAsync<IOException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "ata.pdf", ArquivosDeTeste.Pdf()));

        _documentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Limites (RN40, RN41) ----------

    [Fact]
    public async Task EnviarAsync_de_norma_com_9_existentes_e_aceito()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarContagem(TipoDocumentoPredio.Norma, 9);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, "n.pdf", ArquivosDeTeste.Pdf());

        Assert.Single(_storage.Arquivos);
    }

    [Fact]
    public async Task EnviarAsync_de_norma_com_10_existentes_lanca_LimiteNormasAtingidoException_sem_gravar_nada()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarContagem(TipoDocumentoPredio.Norma, 10);

        await Assert.ThrowsAsync<LimiteNormasAtingidoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, "n.pdf", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_de_norma_conta_todas_as_normas_do_predio_sem_recorte_de_mes()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, "n.pdf", ArquivosDeTeste.Pdf());

        _documentoRepository.Verify(
            r => r.ContarAsync(PredioId, TipoDocumentoPredio.Norma, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_de_ata_com_4_no_mes_e_aceito()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarContagem(TipoDocumentoPredio.Ata, 4);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf());

        Assert.Single(_storage.Arquivos);
    }

    [Fact]
    public async Task EnviarAsync_de_ata_com_5_no_mes_lanca_LimiteAtasMensalAtingidoException_sem_gravar_nada()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarContagem(TipoDocumentoPredio.Ata, 5);

        await Assert.ThrowsAsync<LimiteAtasMensalAtingidoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf()));

        NaoEnviou();
    }

    [Fact]
    public async Task EnviarAsync_de_ata_conta_o_mes_corrente_no_fuso_da_aplicacao()
    {
        // 17/10/2026 01:30 UTC = 16/10/2026 22:30 em UTC-3: o mês é outubro, de 01/10 00:00 a 01/11 00:00 (UTC-3).
        ConfigurarVinculo(PapelPredio.Sindico);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf());

        _documentoRepository.Verify(
            r => r.ContarAsync(
                PredioId,
                TipoDocumentoPredio.Ata,
                new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_de_ata_na_virada_do_mes_usa_o_mes_do_fuso_e_nao_o_do_UTC()
    {
        // 01/11/2026 01:00 UTC ainda é 31/10 22:00 em UTC-3: continua valendo o mês de outubro.
        var sut = CriarSut(Relogio(new DateTimeOffset(2026, 11, 1, 1, 0, 0, TimeSpan.Zero)));
        ConfigurarVinculo(PapelPredio.Sindico);

        await sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf());

        _documentoRepository.Verify(
            r => r.ContarAsync(
                PredioId,
                TipoDocumentoPredio.Ata,
                new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_de_ata_em_dezembro_vira_o_ano_no_proximo_mes()
    {
        var sut = CriarSut(Relogio(new DateTimeOffset(2026, 12, 15, 12, 0, 0, TimeSpan.Zero)));
        ConfigurarVinculo(PapelPredio.Sindico);

        await sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf());

        _documentoRepository.Verify(
            r => r.ContarAsync(
                PredioId,
                TipoDocumentoPredio.Ata,
                new DateTime(2026, 12, 1, 3, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 1, 1, 3, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_limite_de_normas_nao_afeta_atas_e_vice_versa()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarContagem(TipoDocumentoPredio.Norma, 10);

        await _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, "a.pdf", ArquivosDeTeste.Pdf());

        Assert.Single(_storage.Arquivos);
    }

    [Fact]
    public async Task EnviarAsync_arquivo_invalido_e_recusado_antes_de_consultar_o_limite()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<ArquivoDocumentoInvalidoException>(
            () => _sut.EnviarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, "virus.exe", ArquivosDeTeste.Pdf()));

        _documentoRepository.Verify(
            r => r.ContarAsync(It.IsAny<int>(), It.IsAny<TipoDocumentoPredio>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- Listar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ListarAsync_qualquer_papel_vinculado_lista_e_repassa_tipo_nome_e_paginacao(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        _documentoRepository
            .Setup(r => r.ListarAsync(PredioId, TipoDocumentoPredio.Norma, "regimento", 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Documento> { CriarDocumento() }, 11));

        var resultado = await _sut.ListarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, "regimento", 2, 5);

        Assert.Equal(11, resultado.TotalCount);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(DocumentoId, Assert.Single(resultado.Items).Id);
        Assert.Equal("Predio 10", resultado.Items[0].PredioNome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListarAsync_com_busca_vazia_nao_filtra_por_nome(string? nome)
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        _documentoRepository
            .Setup(r => r.ListarAsync(PredioId, TipoDocumentoPredio.Ata, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Documento>(), 0));

        await _sut.ListarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, nome, 1, 10);

        _documentoRepository.Verify(
            r => r.ListarAsync(PredioId, TipoDocumentoPredio.Ata, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ListarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, null, 1, 10));
    }

    [Fact]
    public async Task ListarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ListarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, null, 1, 10));
    }

    // ---------- Obter ----------

    [Fact]
    public async Task ObterAsync_devolve_o_documento()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarDocumento();

        var resultado = await _sut.ObterAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId);

        Assert.Equal(DocumentoId, resultado.Id);
    }

    [Fact]
    public async Task ObterAsync_de_documento_de_outro_predio_lanca_DocumentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarDocumento(predioId: OutroPredioId);

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    [Fact]
    public async Task ObterAsync_de_ata_pela_rota_de_normas_lanca_DocumentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarDocumento(tipo: TipoDocumentoPredio.Ata);

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, TipoDocumentoPredio.Norma, DocumentoId));
    }

    [Fact]
    public async Task ObterAsync_inexistente_lanca_DocumentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    [Fact]
    public async Task ObterAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    // ---------- Baixar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task BaixarAsync_devolve_o_conteudo_gravado_com_nome_e_content_type(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var documento = ConfigurarDocumento();
        _storage.Arquivos[documento.UrlArquivo] = ([1, 2, 3], "application/pdf");

        var arquivo = await _sut.BaixarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId);

        using var conteudo = new MemoryStream();
        await arquivo.Conteudo.CopyToAsync(conteudo);
        Assert.Equal("ata.pdf", arquivo.NomeArquivo);
        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, conteudo.ToArray());
    }

    [Fact]
    public async Task BaixarAsync_com_arquivo_ausente_no_armazenamento_lanca_DocumentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarDocumento();

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.BaixarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    [Fact]
    public async Task BaixarAsync_de_outro_tipo_lanca_DocumentoNaoEncontradoException_sem_ler_o_arquivo()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var documento = ConfigurarDocumento(tipo: TipoDocumentoPredio.Norma);
        _storage.Arquivos[documento.UrlArquivo] = ([1], "application/pdf");

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.BaixarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    [Fact]
    public async Task BaixarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.BaixarAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    // ---------- Excluir ----------

    [Fact]
    public async Task ExcluirAsync_do_Sindico_remove_o_registro_e_o_arquivo()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var documento = ConfigurarDocumento();
        _storage.Arquivos[documento.UrlArquivo] = ([1], "application/pdf");

        await _sut.ExcluirAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId);

        _documentoRepository.Verify(r => r.RemoverAsync(documento, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_storage.Arquivos);
    }

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ExcluirAsync_de_outro_papel_lanca_AcaoNaoPermitidaException_e_nao_apaga_nada(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var documento = ConfigurarDocumento();
        _storage.Arquivos[documento.UrlArquivo] = ([1], "application/pdf");

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.ExcluirAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));

        _documentoRepository.Verify(r => r.RemoverAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(_storage.Arquivos);
    }

    [Fact]
    public async Task ExcluirAsync_de_documento_de_outro_tipo_ou_predio_lanca_DocumentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarDocumento(tipo: TipoDocumentoPredio.Norma);

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => _sut.ExcluirAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));

        _documentoRepository.Verify(r => r.RemoverAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExcluirAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ExcluirAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId));
    }

    [Fact]
    public async Task ExcluirAsync_se_o_armazenamento_falha_ainda_conclui()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var documento = ConfigurarDocumento();
        var storage = new Mock<IFileStorageService>();
        storage
            .Setup(s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("s3 fora"));
        var sut = new DocumentoService(
            _documentoRepository.Object,
            storage.Object,
            new AutorizacaoPredioService(_predioUsuarioRepository.Object),
            TimeProvider.System,
            NullLogger<DocumentoService>.Instance);

        await sut.ExcluirAsync(UsuarioId, PredioId, TipoDocumentoPredio.Ata, DocumentoId);

        _documentoRepository.Verify(r => r.RemoverAsync(documento, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Auxiliares ----------

    private DocumentoService CriarSut(TimeProvider relogio) => new(
        _documentoRepository.Object,
        _storage,
        new AutorizacaoPredioService(_predioUsuarioRepository.Object),
        relogio,
        NullLogger<DocumentoService>.Instance);

    // Qualquer recorte de mês devolve a mesma contagem: o que muda é o tipo.
    private void ConfigurarContagem(TipoDocumentoPredio tipo, int quantidade) =>
        _documentoRepository
            .Setup(r => r.ContarAsync(PredioId, tipo, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quantidade);

    private static RelogioFixo Relogio(DateTimeOffset agora) =>
        new(agora, TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste"));

    private sealed class RelogioFixo(DateTimeOffset agora, TimeZoneInfo fuso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;

        public override TimeZoneInfo LocalTimeZone => fuso;
    }

    private void ConfigurarVinculo(PapelPredio papel, bool predioExcluido = false)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(PredioId, UsuarioId)
            : PredioUsuario.CriarComoConvidado(PredioId, UsuarioId, papel, convidadoPorUsuarioId: 99);

        var predio = new Predio($"Predio {PredioId}", "Rua X, 100", usuarioId: 99);
        if (predioExcluido)
        {
            predio.ExcluirLogicamente();
        }

        Definir(vinculo, nameof(PredioUsuario.Predio), predio);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
    }

    private void SemVinculo() =>
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

    private Documento ConfigurarDocumento(int predioId = PredioId, TipoDocumentoPredio tipo = TipoDocumentoPredio.Ata)
    {
        var documento = CriarDocumento(predioId, tipo);
        _documentoRepository
            .Setup(r => r.BuscarPorIdAsync(DocumentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documento);
        return documento;
    }

    private static Documento CriarDocumento(int predioId = PredioId, TipoDocumentoPredio tipo = TipoDocumentoPredio.Ata)
    {
        var documento = new Documento("ata.pdf", $"documentos/{predioId}/abc.pdf", (int)tipo, predioId);
        Definir(documento, nameof(Documento.Id), DocumentoId);
        return documento;
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);

    private void NaoEnviou()
    {
        _documentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Documento>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_storage.Arquivos);
    }
}
