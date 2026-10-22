using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Planejamentos;
using euSindico.Application.Planejamentos.Dtos;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="PlanejamentoController"/> com um <see cref="PlanejamentoService"/> real e os
/// repositórios mockados (mesmo padrão de CompromissoControllerTests). O id do usuário vem da claim "sub".
/// </summary>
public class PlanejamentoControllerTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;

    private readonly Mock<IPlanejamentoRepository> _planejamentoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly PlanejamentoController _sut;

    public PlanejamentoControllerTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        var service = new PlanejamentoService(_planejamentoRepository.Object, autorizacao, TimeProvider.System);

        var vinculo = PredioUsuario.CriarComoDono(PredioId, UsuarioId);
        Definir(vinculo, nameof(PredioUsuario.Predio), new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioId.ToString()) };
        _sut = new PlanejamentoController(service, new PlanejamentoFormDtoValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) },
            },
        };
    }

    private static PlanejamentoFormDto FormValido() => new("Pintura da fachada", null, 1500m, null, null);

    [Fact]
    public async Task Criar_com_dados_validos_retorna_201_com_location_e_corpo()
    {
        var resultado = await _sut.Criar(PredioId, FormValido(), CancellationToken.None);

        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
        Assert.Equal(nameof(PlanejamentoController.Obter), criado.ActionName);
        Assert.Equal(PredioId, criado.RouteValues!["predioId"]);
        var corpo = Assert.IsType<PlanejamentoDto>(criado.Value);
        Assert.Equal("Pintura da fachada", corpo.Titulo);
        Assert.Equal(PredioId, corpo.PredioId);
    }

    [Fact]
    public async Task Criar_com_titulo_invalido_retorna_400_e_nao_persiste()
    {
        var resultado = await _sut.Criar(PredioId, FormValido() with { Titulo = "<script>" }, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _planejamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Planejamento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Atualizar_com_orcamento_negativo_retorna_400()
    {
        var resultado = await _sut.Atualizar(PredioId, 1, FormValido() with { OrcamentoPrevisto = -1m }, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_defaults_chama_o_service_com_pagina_1_e_tamanho_10()
    {
        PlanejamentoFiltro? capturado = null;
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .Callback((PlanejamentoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Planejamento>(), 0));

        var resultado = await _sut.Listar(PredioId);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.IsType<PagedResultDto<PlanejamentoDto>>(ok.Value);
        Assert.Equal(new PlanejamentoFiltro(PredioId, null, null, null), capturado);
    }

    [Fact]
    public async Task Listar_repassa_titulo_e_faixa_de_orcamento()
    {
        PlanejamentoFiltro? capturado = null;
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .Callback((PlanejamentoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Planejamento>(), 0));

        await _sut.Listar(PredioId, titulo: "elevador", orcamentoMinimo: 100m, orcamentoMaximo: 500m);

        Assert.Equal(new PlanejamentoFiltro(PredioId, "elevador", 100m, 500m), capturado);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 21)]
    public async Task Listar_com_paginacao_fora_da_faixa_retorna_400(int page, int pageSize)
    {
        var resultado = await _sut.Listar(PredioId, page: page, pageSize: pageSize);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_page_size_20_e_aceito()
    {
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Planejamento>(), 0));

        var resultado = await _sut.Listar(PredioId, pageSize: 20);

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task Listar_com_titulo_acima_de_150_caracteres_retorna_400()
    {
        var resultado = await _sut.Listar(PredioId, titulo: new string('a', 151));

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(resultado).StatusCode);
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    [InlineData(500, 100)]
    public async Task Listar_com_faixa_de_orcamento_invalida_retorna_400(int? minimo, int? maximo)
    {
        var resultado = await _sut.Listar(PredioId, orcamentoMinimo: minimo, orcamentoMaximo: maximo);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task Listar_com_minimo_igual_ao_maximo_e_aceito()
    {
        _planejamentoRepository
            .Setup(r => r.ListarAsync(It.IsAny<PlanejamentoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Planejamento>(), 0));

        var resultado = await _sut.Listar(PredioId, orcamentoMinimo: 100m, orcamentoMaximo: 100m);

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task Remover_retorna_204()
    {
        var planejamento = new Planejamento("Pintura", PredioId);
        _planejamentoRepository
            .Setup(r => r.BuscarPorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planejamento);

        var resultado = await _sut.Remover(PredioId, 7, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _planejamentoRepository.Verify(r => r.RemoverAsync(planejamento, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);
}
