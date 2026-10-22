using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Relatorios;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Application.Tests.Relatorios;

public class RelatorioServiceTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;
    private const int OutroPredioId = 20;
    private const int RelatorioId = 100;

    // Relógio: 17/10/2026 01:30 UTC = 16/10/2026 22:30 em UTC-3. "Agora" no fuso da aplicação é outubro/2026.
    private readonly Mock<IRelatorioRepository> _relatorioRepository = new();
    private readonly Mock<ICompromissoRepository> _compromissoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly InMemoryFileStorageService _storage = new();
    private readonly RelatorioPdfGenerator _pdfGenerator = new();
    private readonly RelatorioService _sut;

    public RelatorioServiceTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);

        var fuso = TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste");
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 10, 17, 1, 30, 0, TimeSpan.Zero), fuso);

        _sut = new RelatorioService(
            _relatorioRepository.Object,
            _compromissoRepository.Object,
            _storage,
            _pdfGenerator,
            autorizacao,
            relogio,
            NullLogger<RelatorioService>.Instance);
    }

    // ---------- Gerar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task GerarAsync_persiste_o_registro_e_grava_o_pdf(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        ConfigurarConcluidos(Concluido("Visita técnica", new DateOnly(2026, 9, 3)));

        var resultado = await _sut.GerarAsync(UsuarioId, PredioId, 9, 2026);

        Assert.Equal(9, resultado.MesReferencia);
        Assert.Equal(2026, resultado.AnoReferencia);
        Assert.Equal("Predio-10_Relatorio_2026-09.pdf", resultado.NomeArquivo);
        Assert.Equal("Predio 10", resultado.PredioNome);

        _relatorioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Relatorio>(x => x.PredioId == PredioId && x.MesReferencia == 9 && x.AnoReferencia == 2026),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var (chave, arquivo) = Assert.Single(_storage.Arquivos);
        Assert.StartsWith($"relatorios/{PredioId}/", chave);
        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(arquivo.Conteudo, 0, 4));
    }

    [Fact]
    public async Task GerarAsync_consulta_o_mes_e_ano_pedidos_do_predio_RN06()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));

        await _sut.GerarAsync(UsuarioId, PredioId, 9, 2026);

        _compromissoRepository.Verify(
            r => r.ListarConcluidosDoMesAsync(PredioId, 2026, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GerarAsync_do_mes_corrente_e_aceito()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 10, 1)));

        var resultado = await _sut.GerarAsync(UsuarioId, PredioId, 10, 2026);

        Assert.Equal(10, resultado.MesReferencia);
    }

    [Theory]
    [InlineData(11, 2026)]
    [InlineData(1, 2027)]
    public async Task GerarAsync_de_mes_futuro_lanca_PeriodoRelatorioFuturoException(int mes, int ano)
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<PeriodoRelatorioFuturoException>(() => _sut.GerarAsync(UsuarioId, PredioId, mes, ano));
        NaoGerou();
    }

    [Fact]
    public async Task GerarAsync_sem_compromissos_concluidos_lanca_RelatorioSemCompromissosException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos();

        await Assert.ThrowsAsync<RelatorioSemCompromissosException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));
        NaoGerou();
    }

    [Fact]
    public async Task GerarAsync_duas_vezes_no_mesmo_mes_mantem_o_historico_RN33()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));

        await _sut.GerarAsync(UsuarioId, PredioId, 9, 2026);
        await _sut.GerarAsync(UsuarioId, PredioId, 9, 2026);

        Assert.Equal(2, _storage.Arquivos.Count);
        _relatorioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GerarAsync_se_o_registro_falha_remove_o_arquivo_gravado()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));
        _relatorioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("banco fora"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));

        Assert.Empty(_storage.Arquivos);
    }

    [Fact]
    public async Task GerarAsync_se_o_armazenamento_falha_nao_cria_registro()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));
        _storage.FalharAoSalvar = true;

        await Assert.ThrowsAsync<IOException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));

        _relatorioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GerarAsync_como_Colaborador_lanca_AcaoNaoPermitidaException()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));
        NaoGerou();
    }

    [Fact]
    public async Task GerarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));
        NaoGerou();
    }

    [Fact]
    public async Task GerarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException_RN09()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));
        NaoGerou();
    }

    // ---------- Limite de gerações (RN36) ----------

    [Fact]
    public async Task GerarAsync_com_limite_atingido_lanca_LimiteRelatoriosAtingidoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));
        _relatorioRepository
            .Setup(r => r.ContarAsync(PredioId, 2026, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RelatorioService.LimiteGeracoesPorMes);

        var ex = await Assert.ThrowsAsync<LimiteRelatoriosAtingidoException>(
            () => _sut.GerarAsync(UsuarioId, PredioId, 9, 2026));

        Assert.Contains("3", ex.Message);
        NaoGerou();
    }

    [Fact]
    public async Task GerarAsync_abaixo_do_limite_gera()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarConcluidos(Concluido("Visita", new DateOnly(2026, 9, 3)));
        _relatorioRepository
            .Setup(r => r.ContarAsync(PredioId, 2026, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RelatorioService.LimiteGeracoesPorMes - 1);

        var resultado = await _sut.GerarAsync(UsuarioId, PredioId, 9, 2026);

        Assert.Equal(9, resultado.MesReferencia);
    }

    // ---------- Excluir ----------

    [Fact]
    public async Task ExcluirAsync_como_Sindico_remove_registro_e_arquivo()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var relatorio = ConfigurarRelatorio();
        _storage.Arquivos[relatorio.UrlArquivo] = ([1], "application/pdf");

        await _sut.ExcluirAsync(UsuarioId, PredioId, RelatorioId);

        _relatorioRepository.Verify(r => r.RemoverAsync(relatorio, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_storage.Arquivos);
    }

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ExcluirAsync_sem_ser_Sindico_lanca_AcaoNaoPermitidaException(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        ConfigurarRelatorio();

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ExcluirAsync(UsuarioId, PredioId, RelatorioId));
        _relatorioRepository.Verify(r => r.RemoverAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExcluirAsync_de_relatorio_de_outro_predio_lanca_RelatorioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarRelatorio(predioId: OutroPredioId);

        await Assert.ThrowsAsync<RelatorioNaoEncontradoException>(() => _sut.ExcluirAsync(UsuarioId, PredioId, RelatorioId));
        _relatorioRepository.Verify(r => r.RemoverAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExcluirAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.ExcluirAsync(UsuarioId, PredioId, RelatorioId));
    }

    [Fact]
    public async Task ExcluirAsync_se_o_armazenamento_falha_ainda_conclui()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var relatorio = ConfigurarRelatorio();
        var storage = new Mock<IFileStorageService>();
        storage
            .Setup(s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("s3 fora"));
        var sut = new RelatorioService(
            _relatorioRepository.Object,
            _compromissoRepository.Object,
            storage.Object,
            _pdfGenerator,
            new AutorizacaoPredioService(_predioUsuarioRepository.Object),
            TimeProvider.System,
            NullLogger<RelatorioService>.Instance);

        await sut.ExcluirAsync(UsuarioId, PredioId, RelatorioId);

        _relatorioRepository.Verify(r => r.RemoverAsync(relatorio, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Listar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ListarAsync_qualquer_papel_vinculado_lista_e_repassa_os_filtros(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        _relatorioRepository
            .Setup(r => r.ListarAsync(PredioId, 2026, 9, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Relatorio> { CriarRelatorio() }, 11));

        var resultado = await _sut.ListarAsync(UsuarioId, PredioId, 2026, 9, 2, 5);

        Assert.Equal(11, resultado.TotalCount);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(RelatorioId, Assert.Single(resultado.Items).Id);
        Assert.Equal("Predio 10", resultado.Items[0].PredioNome);
    }

    [Fact]
    public async Task ListarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.ListarAsync(UsuarioId, PredioId, null, null, 1, 10));
    }

    // ---------- Obter ----------

    [Fact]
    public async Task ObterAsync_devolve_o_relatorio()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarRelatorio();

        var resultado = await _sut.ObterAsync(UsuarioId, PredioId, RelatorioId);

        Assert.Equal(RelatorioId, resultado.Id);
    }

    [Fact]
    public async Task ObterAsync_de_relatorio_de_outro_predio_lanca_RelatorioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarRelatorio(predioId: OutroPredioId);

        await Assert.ThrowsAsync<RelatorioNaoEncontradoException>(() => _sut.ObterAsync(UsuarioId, PredioId, RelatorioId));
    }

    [Fact]
    public async Task ObterAsync_inexistente_lanca_RelatorioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<RelatorioNaoEncontradoException>(() => _sut.ObterAsync(UsuarioId, PredioId, RelatorioId));
    }

    // ---------- Baixar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task BaixarAsync_devolve_o_conteudo_gravado(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var relatorio = ConfigurarRelatorio();
        _storage.Arquivos[relatorio.UrlArquivo] = ([1, 2, 3], "application/pdf");

        var arquivo = await _sut.BaixarAsync(UsuarioId, PredioId, RelatorioId);

        using var conteudo = new MemoryStream();
        await arquivo.Conteudo.CopyToAsync(conteudo);
        Assert.Equal("Relatorio_2026-09.pdf", arquivo.NomeArquivo);
        Assert.Equal(new byte[] { 1, 2, 3 }, conteudo.ToArray());
    }

    [Fact]
    public async Task BaixarAsync_com_arquivo_ausente_no_armazenamento_lanca_RelatorioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarRelatorio();

        await Assert.ThrowsAsync<RelatorioNaoEncontradoException>(() => _sut.BaixarAsync(UsuarioId, PredioId, RelatorioId));
    }

    [Fact]
    public async Task BaixarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.BaixarAsync(UsuarioId, PredioId, RelatorioId));
    }

    // ---------- Auxiliares ----------

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

    private void ConfigurarConcluidos(params Compromisso[] compromissos) =>
        _compromissoRepository
            .Setup(r => r.ListarConcluidosDoMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compromissos);

    private static Compromisso Concluido(string titulo, DateOnly data)
    {
        var compromisso = new Compromisso(titulo, data, new TimeOnly(14, 30), PredioId, UsuarioId, "Atlas Ltda.", "Levar laudo");
        compromisso.Concluir();
        Definir(compromisso, nameof(Compromisso.Responsavel), new Usuario("Maria Souza", "m@x.com", "hash"));
        return compromisso;
    }

    private Relatorio ConfigurarRelatorio(int predioId = PredioId)
    {
        var relatorio = CriarRelatorio(predioId);
        _relatorioRepository
            .Setup(r => r.BuscarPorIdAsync(RelatorioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatorio);
        return relatorio;
    }

    private static Relatorio CriarRelatorio(int predioId = PredioId)
    {
        var relatorio = new Relatorio(9, 2026, "Relatorio_2026-09.pdf", $"relatorios/{predioId}/abc.pdf", predioId);
        Definir(relatorio, nameof(Relatorio.Id), RelatorioId);
        return relatorio;
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);

    private void NaoGerou()
    {
        _relatorioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Relatorio>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_storage.Arquivos);
    }

    private sealed class RelogioFixo(DateTimeOffset agora, TimeZoneInfo fuso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;

        public override TimeZoneInfo LocalTimeZone => fuso;
    }
}
