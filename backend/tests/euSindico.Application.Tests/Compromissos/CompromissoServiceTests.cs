using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Compromissos;
using euSindico.Application.Compromissos.Dtos;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Moq;

namespace euSindico.Application.Tests.Compromissos;

public class CompromissoServiceTests
{
    private const int UsuarioId = 1;
    private const int OutroUsuarioId = 2;
    private const int PredioId = 10;
    private const int OutroPredioId = 20;
    private const int CompromissoId = 100;

    private static readonly DateOnly Data = new(2026, 10, 17);
    private static readonly TimeOnly Horario = new(14, 30);

    private readonly Mock<ICompromissoRepository> _compromissoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly CompromissoService _sut;

    public CompromissoServiceTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);

        // 17/10/2026 01:30 UTC = 16/10/2026 22:30 em UTC-3 (Brasília): "hoje" difere do dia em UTC.
        var fuso = TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste");
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 10, 17, 1, 30, 0, TimeSpan.Zero), fuso);

        _sut = new CompromissoService(_compromissoRepository.Object, _predioUsuarioRepository.Object, autorizacao, relogio);
    }

    // ---------- Criar ----------

    [Fact]
    public async Task CriarAsync_sem_responsavel_usa_o_proprio_autor_e_persiste_com_dados_normalizados()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var dto = Form(titulo: "  Visita técnica  ", prestador: "   ", detalhes: "  Revisar  ");

        var resultado = await _sut.CriarAsync(UsuarioId, dto);

        Assert.Equal("Visita técnica", resultado.Titulo);
        Assert.Equal(UsuarioId, resultado.ResponsavelUsuarioId);
        Assert.Equal("Usuario 1", resultado.ResponsavelNome);
        Assert.Equal("Predio 10", resultado.PredioNome);
        Assert.False(resultado.Concluido);
        _compromissoRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Compromisso>(c =>
                    c.Titulo == "Visita técnica"
                    && c.NomePrestadorServico == null
                    && c.Detalhes == "Revisar"
                    && c.PredioId == PredioId
                    && c.ResponsavelUsuarioId == UsuarioId
                    && c.DataCompromisso == Data
                    && c.HorarioCompromisso == Horario),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task CriarAsync_com_responsavel_membro_do_predio_atribui_a_ele(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        ConfigurarVinculo(PapelPredio.Colaborador, usuarioId: OutroUsuarioId);

        var resultado = await _sut.CriarAsync(UsuarioId, Form(responsavel: OutroUsuarioId));

        Assert.Equal(OutroUsuarioId, resultado.ResponsavelUsuarioId);
        Assert.Equal("Usuario 2", resultado.ResponsavelNome);
    }

    [Fact]
    public async Task CriarAsync_com_responsavel_fora_da_equipe_lanca_ResponsavelInvalido_e_nao_persiste()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, OutroUsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

        await Assert.ThrowsAsync<ResponsavelInvalidoException>(() => _sut.CriarAsync(UsuarioId, Form(responsavel: OutroUsuarioId)));

        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_como_Colaborador_sempre_atribui_a_si_mesmo()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        var resultado = await _sut.CriarAsync(UsuarioId, Form());

        Assert.Equal(UsuarioId, resultado.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task CriarAsync_como_Colaborador_aceita_o_proprio_id_como_responsavel()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        var resultado = await _sut.CriarAsync(UsuarioId, Form(responsavel: UsuarioId));

        Assert.Equal(UsuarioId, resultado.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task CriarAsync_como_Colaborador_com_outro_responsavel_lanca_AcaoNaoPermitida_e_nao_persiste()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.CriarAsync(UsuarioId, Form(responsavel: OutroUsuarioId)));

        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_sem_vinculo_lanca_PredioNaoEncontrado()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.CriarAsync(UsuarioId, Form()));

        NaoPersistiu();
    }

    [Fact]
    public async Task CriarAsync_em_predio_excluido_lanca_PredioNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.CriarAsync(UsuarioId, Form()));

        NaoPersistiu();
    }

    // ---------- Listar ----------

    [Fact]
    public async Task ListarAsync_resolve_hoje_e_agora_no_fuso_configurado_e_repassa_os_filtros()
    {
        ConfigurarVinculo(PapelPredio.Gestor);
        CompromissoFiltro? capturado = null;
        _compromissoRepository
            .Setup(r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), 2, 5, It.IsAny<CancellationToken>()))
            .Callback((CompromissoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Compromisso>(), 0));

        await _sut.ListarAsync(UsuarioId, "  visita ", PredioId, EscopoCompromissos.Todos, SituacaoCompromissos.Atrasados, 2, 5);

        Assert.NotNull(capturado);
        Assert.Equal(new DateOnly(2026, 10, 16), capturado.Hoje);
        Assert.Equal(new TimeOnly(22, 30), capturado.Agora);
        Assert.Equal(UsuarioId, capturado.UsuarioId);
        Assert.Equal("visita", capturado.Titulo);
        Assert.Equal(PredioId, capturado.PredioId);
        Assert.Equal(EscopoCompromissos.Todos, capturado.Escopo);
        Assert.Equal(SituacaoCompromissos.Atrasados, capturado.Situacao);
    }

    [Fact]
    public async Task ListarAsync_com_titulo_em_branco_nao_filtra_por_titulo()
    {
        CompromissoFiltro? capturado = null;
        _compromissoRepository
            .Setup(r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .Callback((CompromissoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Compromisso>(), 0));

        await _sut.ListarAsync(UsuarioId, "   ", null, EscopoCompromissos.Meus, SituacaoCompromissos.Proximos, 1, 10);

        Assert.Null(capturado!.Titulo);
        Assert.Null(capturado.PredioId);
    }

    [Fact]
    public async Task ListarAsync_retorna_itens_paginados_com_nomes_do_predio_e_do_responsavel()
    {
        var compromisso = CriarCompromisso(UsuarioId, PredioId);
        _compromissoRepository
            .Setup(r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Compromisso> { compromisso }, 23));

        var resultado = await _sut.ListarAsync(UsuarioId, null, null, EscopoCompromissos.Meus, SituacaoCompromissos.Proximos, 1, 10);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(CompromissoId, item.Id);
        Assert.Equal("Predio 10", item.PredioNome);
        Assert.Equal("Usuario 1", item.ResponsavelNome);
        Assert.Equal(23, resultado.TotalCount);
        Assert.Equal(3, resultado.TotalPages);
        Assert.IsType<PagedResultDto<CompromissoDto>>(resultado);
    }

    [Fact]
    public async Task ListarAsync_com_predio_sem_vinculo_lanca_PredioNaoEncontrado_sem_consultar()
    {
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() =>
            _sut.ListarAsync(UsuarioId, null, PredioId, EscopoCompromissos.Meus, SituacaoCompromissos.Proximos, 1, 10));

        _compromissoRepository.Verify(
            r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListarAsync_com_predio_excluido_lanca_PredioNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() =>
            _sut.ListarAsync(UsuarioId, null, PredioId, EscopoCompromissos.Meus, SituacaoCompromissos.Proximos, 1, 10));
    }

    // ---------- Obter ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task ObterAsync_de_compromisso_alheio_e_permitido_a_Sindico_e_Gestor(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        var resultado = await _sut.ObterAsync(UsuarioId, CompromissoId);

        Assert.Equal(CompromissoId, resultado.Id);
        Assert.Equal(OutroUsuarioId, resultado.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task ObterAsync_de_compromisso_proprio_e_permitido_ao_Colaborador()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        var resultado = await _sut.ObterAsync(UsuarioId, CompromissoId);

        Assert.Equal(CompromissoId, resultado.Id);
    }

    [Fact]
    public async Task ObterAsync_de_compromisso_alheio_pelo_Colaborador_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ObterAsync(UsuarioId, CompromissoId));
    }

    [Fact]
    public async Task ObterAsync_de_compromisso_inexistente_lanca_CompromissoNaoEncontrado()
    {
        _compromissoRepository
            .Setup(r => r.BuscarPorIdAsync(CompromissoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Compromisso?)null);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.ObterAsync(UsuarioId, CompromissoId));
    }

    [Fact]
    public async Task ObterAsync_sem_vinculo_com_o_predio_lanca_CompromissoNaoEncontrado_igual_ao_inexistente()
    {
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);
        SemVinculo(PredioId, UsuarioId);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.ObterAsync(UsuarioId, CompromissoId));
    }

    [Fact]
    public async Task ObterAsync_em_predio_excluido_lanca_CompromissoNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.ObterAsync(UsuarioId, CompromissoId));
    }

    // ---------- Atualizar ----------

    [Fact]
    public async Task AtualizarAsync_altera_os_dados_e_mantem_o_responsavel_quando_omitido()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);
        var dto = Form(titulo: "  Reunião  ", prestador: "Atlas", detalhes: null, data: new DateOnly(2026, 11, 5), horario: new TimeOnly(8, 0));

        var resultado = await _sut.AtualizarAsync(UsuarioId, CompromissoId, dto);

        Assert.Equal("Reunião", compromisso.Titulo);
        Assert.Equal("Atlas", compromisso.NomePrestadorServico);
        Assert.Null(compromisso.Detalhes);
        Assert.Equal(new DateOnly(2026, 11, 5), compromisso.DataCompromisso);
        Assert.Equal(new TimeOnly(8, 0), compromisso.HorarioCompromisso);
        Assert.Equal(OutroUsuarioId, resultado.ResponsavelUsuarioId);
        Assert.Equal("Usuario 2", resultado.ResponsavelNome);
        _compromissoRepository.Verify(r => r.AtualizarAsync(compromisso, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_sem_mudar_responsavel_nem_predio_nao_revalida_a_equipe()
    {
        // RN22: o responsável pode já ter saído da equipe — isso não deve impedir editar o título.
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);
        SemVinculo(PredioId, OutroUsuarioId);

        var resultado = await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(responsavel: OutroUsuarioId));

        Assert.Equal(OutroUsuarioId, resultado.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task AtualizarAsync_de_compromisso_concluido_e_permitido()
    {
        ConfigurarVinculo(PapelPredio.Gestor);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);
        compromisso.Concluir();

        await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(titulo: "Novo título"));

        Assert.Equal("Novo título", compromisso.Titulo);
        Assert.True(compromisso.Concluido);
    }

    [Fact]
    public async Task AtualizarAsync_reatribui_a_outro_membro_do_predio()
    {
        ConfigurarVinculo(PapelPredio.Gestor);
        ConfigurarVinculo(PapelPredio.Colaborador, usuarioId: OutroUsuarioId);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        var resultado = await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(responsavel: OutroUsuarioId));

        Assert.Equal(OutroUsuarioId, compromisso.ResponsavelUsuarioId);
        Assert.Equal("Usuario 2", resultado.ResponsavelNome);
    }

    [Fact]
    public async Task AtualizarAsync_reatribuir_a_quem_nao_e_da_equipe_lanca_ResponsavelInvalido_e_nao_persiste()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);
        SemVinculo(PredioId, OutroUsuarioId);

        await Assert.ThrowsAsync<ResponsavelInvalidoException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(responsavel: OutroUsuarioId)));

        Assert.Equal(UsuarioId, compromisso.ResponsavelUsuarioId);
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_como_Colaborador_no_proprio_compromisso_altera_os_dados()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(titulo: "Trocar lâmpada"));

        Assert.Equal("Trocar lâmpada", compromisso.Titulo);
        Assert.Equal(UsuarioId, compromisso.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task AtualizarAsync_como_Colaborador_em_compromisso_alheio_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form()));

        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_como_Colaborador_tentando_reatribuir_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(responsavel: OutroUsuarioId)));

        Assert.Equal(UsuarioId, compromisso.ResponsavelUsuarioId);
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_como_Colaborador_tentando_mover_de_predio_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarVinculo(PapelPredio.Gestor, predioId: OutroPredioId);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId)));

        Assert.Equal(PredioId, compromisso.PredioId);
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_move_para_outro_predio_onde_e_Gestor_e_o_responsavel_e_membro()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Gestor, predioId: OutroPredioId);
        ConfigurarVinculo(PapelPredio.Colaborador, predioId: OutroPredioId, usuarioId: OutroUsuarioId);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        var resultado = await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId));

        Assert.Equal(OutroPredioId, compromisso.PredioId);
        Assert.Equal(OutroPredioId, resultado.PredioId);
        Assert.Equal("Predio 20", resultado.PredioNome);
        Assert.Equal(OutroUsuarioId, resultado.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task AtualizarAsync_move_para_predio_onde_o_responsavel_atual_nao_e_membro_lanca_ResponsavelInvalido()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Gestor, predioId: OutroPredioId);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);
        SemVinculo(OutroPredioId, OutroUsuarioId);

        await Assert.ThrowsAsync<ResponsavelInvalidoException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId)));

        Assert.Equal(PredioId, compromisso.PredioId);
        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_move_para_predio_trocando_o_responsavel_na_mesma_requisicao()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Sindico, predioId: OutroPredioId);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);
        SemVinculo(OutroPredioId, OutroUsuarioId);

        await _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId, responsavel: UsuarioId));

        Assert.Equal(OutroPredioId, compromisso.PredioId);
        Assert.Equal(UsuarioId, compromisso.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task AtualizarAsync_move_para_predio_sem_vinculo_lanca_PredioNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarCompromisso(responsavelId: UsuarioId);
        SemVinculo(OutroPredioId, UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId)));

        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_move_para_predio_onde_e_apenas_Colaborador_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Colaborador, predioId: OutroPredioId);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId)));

        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_move_para_predio_excluido_lanca_PredioNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        ConfigurarVinculo(PapelPredio.Sindico, predioId: OutroPredioId, predioExcluido: true);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form(predioId: OutroPredioId)));

        NaoAtualizou();
    }

    [Fact]
    public async Task AtualizarAsync_em_predio_excluido_lanca_CompromissoNaoEncontrado()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.AtualizarAsync(UsuarioId, CompromissoId, Form()));

        NaoAtualizou();
    }

    // ---------- Alterar conclusão ----------

    [Fact]
    public async Task AlterarConclusaoAsync_true_conclui_e_false_reabre()
    {
        ConfigurarVinculo(PapelPredio.Gestor);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        var concluido = await _sut.AlterarConclusaoAsync(UsuarioId, CompromissoId, true);
        Assert.True(concluido.Concluido);
        Assert.True(compromisso.Concluido);

        var reaberto = await _sut.AlterarConclusaoAsync(UsuarioId, CompromissoId, false);
        Assert.False(reaberto.Concluido);
        Assert.False(compromisso.Concluido);

        _compromissoRepository.Verify(r => r.AtualizarAsync(compromisso, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AlterarConclusaoAsync_como_Colaborador_no_proprio_compromisso_e_permitido()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        await _sut.AlterarConclusaoAsync(UsuarioId, CompromissoId, true);

        Assert.True(compromisso.Concluido);
    }

    [Fact]
    public async Task AlterarConclusaoAsync_como_Colaborador_em_compromisso_alheio_lanca_AcaoNaoPermitida()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.AlterarConclusaoAsync(UsuarioId, CompromissoId, true));

        Assert.False(compromisso.Concluido);
        NaoAtualizou();
    }

    [Fact]
    public async Task AlterarConclusaoAsync_de_compromisso_inexistente_lanca_CompromissoNaoEncontrado()
    {
        _compromissoRepository
            .Setup(r => r.BuscarPorIdAsync(CompromissoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Compromisso?)null);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.AlterarConclusaoAsync(UsuarioId, CompromissoId, true));
    }

    // ---------- Excluir ----------

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task ExcluirAsync_de_compromisso_alheio_remove_para_Sindico_e_Gestor(PapelPredio papel)
    {
        ConfigurarVinculo(papel);
        var compromisso = ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        await _sut.ExcluirAsync(UsuarioId, CompromissoId);

        _compromissoRepository.Verify(r => r.RemoverAsync(compromisso, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_de_compromisso_concluido_e_permitido()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);
        compromisso.Concluir();

        await _sut.ExcluirAsync(UsuarioId, CompromissoId);

        _compromissoRepository.Verify(r => r.RemoverAsync(compromisso, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_como_Colaborador_no_proprio_compromisso_remove()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        var compromisso = ConfigurarCompromisso(responsavelId: UsuarioId);

        await _sut.ExcluirAsync(UsuarioId, CompromissoId);

        _compromissoRepository.Verify(r => r.RemoverAsync(compromisso, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_como_Colaborador_em_compromisso_alheio_lanca_AcaoNaoPermitida_e_nao_remove()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);
        ConfigurarCompromisso(responsavelId: OutroUsuarioId);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ExcluirAsync(UsuarioId, CompromissoId));

        _compromissoRepository.Verify(r => r.RemoverAsync(It.IsAny<Compromisso>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExcluirAsync_em_predio_excluido_lanca_CompromissoNaoEncontrado_e_nao_remove()
    {
        ConfigurarVinculo(PapelPredio.Sindico, predioExcluido: true);
        ConfigurarCompromisso(responsavelId: UsuarioId);

        await Assert.ThrowsAsync<CompromissoNaoEncontradoException>(() => _sut.ExcluirAsync(UsuarioId, CompromissoId));

        _compromissoRepository.Verify(r => r.RemoverAsync(It.IsAny<Compromisso>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Helpers ----------

    private static CompromissoFormDto Form(
        int predioId = PredioId,
        string titulo = "Visita técnica",
        DateOnly? data = null,
        TimeOnly? horario = null,
        string? prestador = null,
        string? detalhes = null,
        int? responsavel = null) =>
        new(predioId, titulo, data ?? Data, horario ?? Horario, prestador, detalhes, responsavel);

    private void ConfigurarVinculo(PapelPredio papel, int predioId = PredioId, int usuarioId = UsuarioId, bool predioExcluido = false)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId, usuarioId)
            : PredioUsuario.CriarComoConvidado(predioId, usuarioId, papel, convidadoPorUsuarioId: 99);

        var predio = new Predio($"Predio {predioId}", "Rua X, 100", usuarioId: 99);
        if (predioExcluido)
        {
            predio.ExcluirLogicamente();
        }

        Definir(vinculo, nameof(PredioUsuario.Predio), predio);
        Definir(vinculo, nameof(PredioUsuario.Usuario), new Usuario($"Usuario {usuarioId}", $"u{usuarioId}@x.com", "hash"));

        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(predioId, usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
    }

    private void SemVinculo(int predioId, int usuarioId) =>
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(predioId, usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

    private Compromisso ConfigurarCompromisso(int responsavelId, int predioId = PredioId)
    {
        var compromisso = CriarCompromisso(responsavelId, predioId);
        _compromissoRepository
            .Setup(r => r.BuscarPorIdAsync(CompromissoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compromisso);
        return compromisso;
    }

    private static Compromisso CriarCompromisso(int responsavelId, int predioId)
    {
        var compromisso = new Compromisso("Visita técnica", Data, Horario, predioId, responsavelId);
        Definir(compromisso, nameof(Compromisso.Id), CompromissoId);
        Definir(compromisso, nameof(Compromisso.Predio), new Predio($"Predio {predioId}", "Rua X, 100", usuarioId: 99));
        Definir(compromisso, nameof(Compromisso.Responsavel), new Usuario($"Usuario {responsavelId}", $"u{responsavelId}@x.com", "hash"));
        return compromisso;
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);

    private void NaoPersistiu() =>
        _compromissoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Compromisso>(), It.IsAny<CancellationToken>()), Times.Never);

    private void NaoAtualizou() =>
        _compromissoRepository.Verify(r => r.AtualizarAsync(It.IsAny<Compromisso>(), It.IsAny<CancellationToken>()), Times.Never);

    private sealed class RelogioFixo(DateTimeOffset agora, TimeZoneInfo fuso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;

        public override TimeZoneInfo LocalTimeZone => fuso;
    }
}
