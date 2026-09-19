using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Predios;
using euSindico.Application.Predios.Dtos;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="PredioController"/> com um <see cref="PredioService"/> real e
/// <see cref="IPredioRepository"/>/<see cref="IPredioUsuarioRepository"/> mockados (mesmo
/// padrão de EquipeControllerTests). O id do usuário vem da claim "sub" do token (RN02),
/// nunca do corpo da requisição.
/// </summary>
public class PredioControllerTests
{
    private const int UsuarioId = 1;

    private readonly Mock<IPredioRepository> _predioRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly PredioController _sut;

    public PredioControllerTests()
    {
        var autorizacaoPredioService = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        var predioService = new PredioService(_predioRepository.Object, autorizacaoPredioService);

        _predioRepository.Setup(r => r.ContarAtivosDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _predioRepository
            .Setup(r => r.ExisteNomeEEnderecoAtivoAsync(UsuarioId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _predioRepository
            .Setup(r => r.AdicionarComDonoAsync(It.IsAny<Predio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Predio p, CancellationToken _) => p);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioId.ToString()) };
        _sut = new PredioController(predioService, new CriarPredioDtoValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
                },
            },
        };
    }

    [Fact]
    public async Task Criar_com_dados_validos_retorna_201_com_location_e_corpo()
    {
        var resultado = await _sut.Criar(new CriarPredioDto("Edifício Aurora", "Rua X, 100"), CancellationToken.None);

        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
        Assert.Equal(nameof(PredioController.Obter), criado.ActionName);
        var corpo = Assert.IsType<PredioDto>(criado.Value);
        Assert.Equal("Edifício Aurora", corpo.Nome);
        Assert.Equal(corpo.Id, criado.RouteValues!["id"]);
    }

    [Fact]
    public async Task Criar_com_nome_invalido_retorna_400_e_nao_persiste()
    {
        var resultado = await _sut.Criar(new CriarPredioDto("<script>alert(1)</script>", "Rua X, 100"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _predioRepository.Verify(r => r.AdicionarComDonoAsync(It.IsAny<Predio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Obter_com_vinculo_retorna_200_com_corpo()
    {
        var vinculo = PredioUsuario.CriarComoDono(10, UsuarioId);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(10, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _predioRepository
            .Setup(r => r.BuscarPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));

        var resultado = await _sut.Obter(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<PredioDto>(ok.Value);
        Assert.Equal("Edifício Aurora", corpo.Nome);
    }
}
