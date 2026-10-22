using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Moq;

namespace euSindico.Application.Tests.Equipe;

public class AutorizacaoPredioServiceTests
{
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly AutorizacaoPredioService _sut;

    public AutorizacaoPredioServiceTests()
    {
        _sut = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
    }

    [Fact]
    public async Task VerificarAcessoAsync_sem_vinculo_lanca_PredioNaoEncontradoException()
    {
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

        await Assert.ThrowsAsync<PredioNaoEncontradoException>(
            () => _sut.VerificarAcessoAsync(99, 1, AcaoPredio.GerenciarEquipe));
    }

    [Fact]
    public async Task VerificarAcessoAsync_com_papel_sindico_permite_gerenciar_equipe_e_retorna_o_vinculo()
    {
        var vinculo = PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.VerificarAcessoAsync(10, 1, AcaoPredio.GerenciarEquipe);

        Assert.Equal(PapelPredio.Sindico, resultado.Papel);
        Assert.Same(vinculo, resultado);
    }

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_com_papel_nao_sindico_nao_permite_gerenciar_equipe(PapelPredio papel)
    {
        var vinculo = PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarEquipe));
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_com_qualquer_papel_permite_visualizar_predio(PapelPredio papel)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 10)
            : PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 10, papel, convidadoPorUsuarioId: 1);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.VerificarAcessoAsync(10, 1, AcaoPredio.VisualizarPredio);

        Assert.Equal(papel, resultado.Papel);
    }

    [Fact]
    public async Task VerificarAcessoAsync_com_papel_sindico_permite_gerenciar_predio()
    {
        var vinculo = PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.VerificarAcessoAsync(10, 1, AcaoPredio.GerenciarPredio);

        Assert.Equal(PapelPredio.Sindico, resultado.Papel);
    }

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_com_papel_nao_sindico_nao_permite_gerenciar_predio(PapelPredio papel)
    {
        var vinculo = PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarPredio));
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task VerificarAcessoAsync_permite_visualizar_equipe_a_sindico_e_gestor(PapelPredio papel)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 20)
            : PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.VisualizarEquipe);

        Assert.Equal(papel, resultado.Papel);
    }

    [Fact]
    public async Task VerificarAcessoAsync_colaborador_nao_pode_visualizar_equipe()
    {
        var vinculo = PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, PapelPredio.Colaborador, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.VisualizarEquipe));
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_permite_acessar_compromissos_a_qualquer_papel_vinculado(PapelPredio papel)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 20)
            : PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarCompromissos);

        Assert.Equal(papel, resultado.Papel);
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_permite_visualizar_planejamentos_a_qualquer_papel_vinculado(PapelPredio papel)
    {
        ConfigurarVinculo(papel);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.VisualizarPlanejamentos);

        Assert.Equal(papel, resultado.Papel);
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task VerificarAcessoAsync_permite_gerenciar_planejamentos_a_sindico_e_gestor(PapelPredio papel)
    {
        ConfigurarVinculo(papel);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarPlanejamentos);

        Assert.Equal(papel, resultado.Papel);
    }

    [Fact]
    public async Task VerificarAcessoAsync_colaborador_nao_pode_gerenciar_planejamentos()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarPlanejamentos));
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public async Task VerificarAcessoAsync_permite_visualizar_relatorios_a_qualquer_papel_vinculado(PapelPredio papel)
    {
        ConfigurarVinculo(papel);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.VisualizarRelatorios);

        Assert.Equal(papel, resultado.Papel);
    }

    [Theory]
    [InlineData(PapelPredio.Sindico)]
    [InlineData(PapelPredio.Gestor)]
    public async Task VerificarAcessoAsync_permite_gerar_relatorios_a_sindico_e_gestor(PapelPredio papel)
    {
        ConfigurarVinculo(papel);

        var resultado = await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarRelatorios);

        Assert.Equal(papel, resultado.Papel);
    }

    [Fact]
    public async Task VerificarAcessoAsync_colaborador_nao_pode_gerar_relatorios()
    {
        ConfigurarVinculo(PapelPredio.Colaborador);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.GerenciarRelatorios));
    }

    [Fact]
    public async Task VerificarAcessoAsync_permite_excluir_relatorios_so_ao_sindico()
    {
        ConfigurarVinculo(PapelPredio.Sindico);
        Assert.Equal(PapelPredio.Sindico, (await _sut.VerificarAcessoAsync(20, 1, AcaoPredio.ExcluirRelatorios)).Papel);

        ConfigurarVinculo(PapelPredio.Gestor);
        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(
            () => _sut.VerificarAcessoAsync(20, 1, AcaoPredio.ExcluirRelatorios));
    }

    private void ConfigurarVinculo(PapelPredio papel)
    {
        var vinculo = papel == PapelPredio.Sindico
            ? PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 20)
            : PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
    }
}
