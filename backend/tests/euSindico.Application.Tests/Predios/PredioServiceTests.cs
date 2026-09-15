using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Predios;
using euSindico.Application.Predios.Dtos;
using euSindico.Domain.Entities;
using Moq;

namespace euSindico.Application.Tests.Predios;

public class PredioServiceTests
{
    private const int UsuarioId = 1;

    private readonly Mock<IPredioRepository> _predioRepository = new();
    private readonly PredioService _sut;

    public PredioServiceTests()
    {
        _sut = new PredioService(_predioRepository.Object);

        // Padrão: abaixo do limite e sem duplicidade — os testes que precisam de outro
        // cenário sobrescrevem essas configurações.
        _predioRepository.Setup(r => r.ContarAtivosDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _predioRepository
            .Setup(r => r.ExisteNomeEEnderecoAtivoAsync(UsuarioId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _predioRepository
            .Setup(r => r.AdicionarComDonoAsync(It.IsAny<Predio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Predio p, CancellationToken _) => p);
    }

    [Fact]
    public async Task CriarAsync_com_dados_validos_persiste_e_retorna_dto()
    {
        var dto = new CriarPredioDto("Edifício Aurora", "Rua X, 100");

        var resultado = await _sut.CriarAsync(UsuarioId, dto);

        Assert.Equal("Edifício Aurora", resultado.Nome);
        Assert.Equal("Rua X, 100", resultado.Endereco);

        _predioRepository.Verify(
            r => r.AdicionarComDonoAsync(
                It.Is<Predio>(p => p.Nome == "Edifício Aurora" && p.Endereco == "Rua X, 100" && p.UsuarioId == UsuarioId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_remove_espacos_nas_pontas_de_nome_e_endereco_antes_de_persistir()
    {
        var dto = new CriarPredioDto("  Edifício Aurora  ", "  Rua X, 100  ");

        await _sut.CriarAsync(UsuarioId, dto);

        _predioRepository.Verify(
            r => r.AdicionarComDonoAsync(
                It.Is<Predio>(p => p.Nome == "Edifício Aurora" && p.Endereco == "Rua X, 100"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_com_20_predios_ativos_lanca_PredioLimiteAtingido_e_nao_persiste()
    {
        _predioRepository.Setup(r => r.ContarAtivosDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        var dto = new CriarPredioDto("Edifício Aurora", "Rua X, 100");

        await Assert.ThrowsAsync<PredioLimiteAtingidoException>(() => _sut.CriarAsync(UsuarioId, dto));

        _predioRepository.Verify(r => r.AdicionarComDonoAsync(It.IsAny<Predio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_com_nome_e_endereco_ja_ativos_lanca_PredioDuplicado_e_nao_persiste()
    {
        _predioRepository
            .Setup(r => r.ExisteNomeEEnderecoAtivoAsync(UsuarioId, "Edifício Aurora", "Rua X, 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CriarPredioDto("Edifício Aurora", "Rua X, 100");

        await Assert.ThrowsAsync<PredioDuplicadoException>(() => _sut.CriarAsync(UsuarioId, dto));

        _predioRepository.Verify(r => r.AdicionarComDonoAsync(It.IsAny<Predio>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
