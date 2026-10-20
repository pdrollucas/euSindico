using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Planejamentos;
using euSindico.Application.Planejamentos.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Moq;

namespace euSindico.Application.Tests.Planejamentos;

public class PlanejamentoServiceTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;
    private const int OutroPredioId = 20;
    private const int PlanejamentoId = 100;

    // Relógio: 17/10/2026 01:30 UTC = 16/10/2026 22:30 em UTC-3. "Hoje" no fuso da aplicação é 16/10.
    private static readonly DateOnly Hoje = new(2026, 10, 16);

    private readonly Mock<IPlanejamentoRepository> _planejamentoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly PlanejamentoService _sut;

    public PlanejamentoServiceTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);

        var fuso = TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste");
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 10, 17, 1, 30, 0, TimeSpan.Zero), fuso);

        _sut = new PlanejamentoService(_planejamentoRepository.Object, autorizacao, relogio);
    }

    // ---------- Criar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task CriarAsync_persiste_com_dados_normalizados(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var dto = Form(titulo: "  Pintura  ", prestador: "   ", detalhes: "  Cotar  ", orcamento: 0m);

        var resultado = await _sut.CriarAsync(UsuarioId, PredioId, dto);

        Assert.Equal("Pintura", resultado.Titulo);
        Assert.Equal("Predio 10", resultado.PredioNome);
        Assert.Equal(0m, resultado.OrcamentoPrevisto);
        _planejamentoRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Planejamento>(p =>
                    p.Titulo == "Pintura"
                    && p.NomePrestadorServico == null
                    && p.Detalhes == "Cotar"
                    && p.PredioId == PredioId
                    && p.OrcamentoPrevisto == 0m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_sem_data_e_aceito_RN11()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        var resultado = await _sut.CriarAsync(UsuarioId, PredioId, Form(data: null));

        Assert.Null(resultado.DataPlanejamento);
    }

    [Fact]
    public async Task CriarAsync_com_data_de_hoje_no_fuso_da_aplicacao_e_aceito()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        // Em UTC já seria dia 17: a regra usa o fuso configurado, não o do servidor.
        var resultado = await _sut.CriarAsync(UsuarioId, PredioId, Form(data: Hoje));

        Assert.Equal(Hoje, resultado.DataPlanejamento);
    }

    [Fact]
    public async Task CriarAsync_com_data_anterior_a_hoje_lanca_DataPlanejamentoNoPassadoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);

        await Assert.ThrowsAsync<DataPlanejamentoNoPassadoException>(
            () => _sut.CriarAsync(UsuarioId, PredioId, Form(data: Hoje.AddDays(-1))));
        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_como_Colaborador_lanca_AcaoNaoPermitidaException()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.CriarAsync(UsuarioId, PredioId, Form()));
        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.CriarAsync(UsuarioId, PredioId, Form()));
        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException_RN09()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.CriarAsync(UsuarioId, PredioId, Form()));
        NaoPersistiu();
    }

    // ---------- Listar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ListarAsync_qualquer_papel_vinculado_lista_e_repassa_o_filtro_normalizado(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        PlanejamentoFiltro? capturado = null;
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 2, 5, It.IsAny<CancellationToken>()))
            .Callback((PlanejamentoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Planejamento> { CriarPlanejamento() }, 6));

        var resultado = await _sut.ListarAsync(UsuarioId, PredioId, "  elevador ", 100m, 500m, page: 2, pageSize: 5);

        Assert.Equal(new PlanejamentoFiltro(PredioId, "elevador", 100m, 500m), capturado);
        Assert.Single(resultado.Items);
        Assert.Equal("Predio 10", resultado.Items[0].PredioNome);
        Assert.Equal(6, resultado.TotalCount);
        Assert.Equal(2, resultado.Page);
    }

    [Fact]
    public async Task ListarAsync_titulo_em_branco_vira_sem_filtro()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        PlanejamentoFiltro? capturado = null;
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .Callback((PlanejamentoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Planejamento>(), 0));

        await _sut.ListarAsync(UsuarioId, PredioId, "   ", null, null, 1, 10);

        Assert.Null(capturado!.Titulo);
    }

    [Fact]
    public async Task ListarAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ListarAsync(UsuarioId, PredioId, null, null, null, 1, 10));
    }

    [Fact]
    public async Task ListarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ListarAsync(UsuarioId, PredioId, null, null, null, 1, 10));
    }

    // ---------- Obter ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task ObterAsync_qualquer_papel_vinculado_obtem(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        ConfigurarPlanejamento();

        var resultado = await _sut.ObterAsync(UsuarioId, PredioId, PlanejamentoId);

        Assert.Equal(PlanejamentoId, resultado.Id);
    }

    [Fact]
    public async Task ObterAsync_planejamento_inexistente_lanca_PlanejamentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        _planejamentoRepository
            .Setup(r => r.BuscarPorIdAsync(PlanejamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Planejamento?)null);

        await Assert.ThrowsAsync<PlanejamentoNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, PlanejamentoId));
    }

    [Fact]
    public async Task ObterAsync_planejamento_de_outro_predio_lanca_PlanejamentoNaoEncontradoException()
    {
        // O usuário é Síndico dos dois prédios: ainda assim o id só vale dentro do prédio da rota.
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Sindico, predioId: OutroPredioId);
        ConfigurarPlanejamento(predioId: OutroPredioId);

        await Assert.ThrowsAsync<PlanejamentoNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, PlanejamentoId));
    }

    [Fact]
    public async Task ObterAsync_sem_vinculo_lanca_PredioNaoEncontradoException_sem_consultar_o_planejamento()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.ObterAsync(UsuarioId, PredioId, PlanejamentoId));
        _planejamentoRepository.Verify(
            r => r.BuscarPorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Atualizar ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task AtualizarAsync_altera_os_campos_normalizados_e_persiste(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var planejamento = ConfigurarPlanejamento();
        var nova = Hoje.AddDays(30);

        var resultado = await _sut.AtualizarAsync(
            UsuarioId, PredioId, PlanejamentoId, Form(titulo: "  Reforma  ", data: nova, orcamento: 0m, prestador: " Atlas "));

        Assert.Equal("Reforma", resultado.Titulo);
        Assert.Equal(nova, planejamento.DataPlanejamento);
        Assert.Equal(0m, planejamento.OrcamentoPrevisto);
        Assert.Equal("Atlas", planejamento.NomePrestadorServico);
        Assert.Equal(PredioId, planejamento.PredioId);
        _planejamentoRepository.Verify(r => r.AtualizarAsync(planejamento, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_pode_limpar_a_data()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var planejamento = ConfigurarPlanejamento(data: Hoje.AddDays(5));

        await _sut.AtualizarAsync(UsuarioId, PredioId, PlanejamentoId, Form(data: null));

        Assert.Null(planejamento.DataPlanejamento);
    }

    [Fact]
    public async Task AtualizarAsync_valida_a_data_mesmo_quando_ela_nao_mudou()
    {
        // Um planejamento vencido só pode ser corrigido trocando ou limpando a data.
        ConfigurarVinculo(PapelPredio.Sindico);
        var vencida = Hoje.AddDays(-3);
        ConfigurarPlanejamento(data: vencida);

        await Assert.ThrowsAsync<DataPlanejamentoNoPassadoException>(
            () => _sut.AtualizarAsync(UsuarioId, PredioId, PlanejamentoId, Form(titulo: "Novo título", data: vencida)));
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_como_Colaborador_lanca_AcaoNaoPermitidaException()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarPlanejamento();

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.AtualizarAsync(UsuarioId, PredioId, PlanejamentoId, Form()));
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_planejamento_de_outro_predio_lanca_PlanejamentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarPlanejamento(predioId: OutroPredioId);

        await Assert.ThrowsAsync<PlanejamentoNaoEncontradoException>(
            () => _sut.AtualizarAsync(UsuarioId, PredioId, PlanejamentoId, Form()));
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_em_predio_excluido_lanca_PredioNaoEncontradoException_RN09()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);
        ConfigurarPlanejamento();

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.AtualizarAsync(UsuarioId, PredioId, PlanejamentoId, Form()));
        NaoAtualizou();
    }

    // ---------- Excluir ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task ExcluirAsync_remove_o_planejamento(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var planejamento = ConfigurarPlanejamento();

        await _sut.ExcluirAsync(UsuarioId, PredioId, PlanejamentoId);

        _planejamentoRepository.Verify(r => r.RemoverAsync(planejamento, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_como_Colaborador_lanca_AcaoNaoPermitidaException()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarPlanejamento();

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ExcluirAsync(UsuarioId, PredioId, PlanejamentoId));
        NaoRemoveu();
    }

    [Fact]
    public async Task ExcluirAsync_planejamento_de_outro_predio_lanca_PlanejamentoNaoEncontradoException()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarPlanejamento(predioId: OutroPredioId);

        await Assert.ThrowsAsync<PlanejamentoNaoEncontradoException>(
            () => _sut.ExcluirAsync(UsuarioId, PredioId, PlanejamentoId));
        NaoRemoveu();
    }

    [Fact]
    public async Task ExcluirAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.ExcluirAsync(UsuarioId, PredioId, PlanejamentoId));
        NaoRemoveu();
    }

    // ---------- Auxiliares ----------

    private static PlanejamentoFormDto Form(
        string titulo = "Pintura da fachada",
        DateOnly? data = null,
        decimal? orcamento = null,
        string? prestador = null,
        string? detalhes = null) =>
        new(titulo, data, orcamento, prestador, detalhes);

    private void ConfigurarVinculo(PapelPredio papel, int predioId = PredioId, bool predioExcluido = false)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId, UsuarioId)
            : PredioUsuario.CriarComoConvidado(predioId, UsuarioId, papel, convidadoPorUsuarioId: 99);

        var predio = new Predio($"Predio {predioId}", "Rua X, 100", usuarioId: 99);
        if (predioExcluido)
        {
            predio.ExcluirLogicamente();
        }

        Definir(vinculo, nameof(PredioUsuario.Predio), predio);
        Definir(vinculo, nameof(PredioUsuario.Usuario), new Usuario($"Usuario {UsuarioId}", $"u{UsuarioId}@x.com", "hash"));

        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(predioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
    }

    private void SemVinculo(int predioId, int usuarioId) =>
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(predioId, usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

    private Planejamento ConfigurarPlanejamento(int predioId = PredioId, DateOnly? data = null)
    {
        var planejamento = CriarPlanejamento(predioId, data);
        _planejamentoRepository
            .Setup(r => r.BuscarPorIdAsync(PlanejamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planejamento);
        return planejamento;
    }

    private static Planejamento CriarPlanejamento(int predioId = PredioId, DateOnly? data = null)
    {
        var planejamento = new Planejamento("Pintura da fachada", predioId, data);
        Definir(planejamento, nameof(Planejamento.Id), PlanejamentoId);
        Definir(planejamento, nameof(Planejamento.Predio), new Predio($"Predio {predioId}", "Rua X, 100", usuarioId: 99));
        return planejamento;
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);

    private void NaoPersistiu() =>
        _planejamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Planejamento>(), It.IsAny<CancellationToken>()), Times.Never);

    private void NaoAtualizou() =>
        _planejamentoRepository.Verify(r => r.AtualizarAsync(It.IsAny<Planejamento>(), It.IsAny<CancellationToken>()), Times.Never);

    private void NaoRemoveu() =>
        _planejamentoRepository.Verify(r => r.RemoverAsync(It.IsAny<Planejamento>(), It.IsAny<CancellationToken>()), Times.Never);

    private sealed class RelogioFixo(DateTimeOffset agora, TimeZoneInfo fuso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;

        public override TimeZoneInfo LocalTimeZone => fuso;
    }
}
