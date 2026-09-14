using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Equipe.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="ConviteController"/> com um <see cref="EquipeService"/> real e
/// repositórios mockados — nenhuma rota exige autenticação (RF30), então, diferente de
/// EquipeControllerTests, não há claim de usuário para configurar.
/// </summary>
public class ConviteControllerTests
{
    private const int SindicoId = 1;
    private const int PredioId = 10;

    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly Mock<IConviteFuncionarioRepository> _conviteFuncionarioRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IConviteLinkBuilder> _conviteLinkBuilder = new();
    private readonly ConviteController _sut;

    public ConviteControllerTests()
    {
        var autorizacaoPredioService = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        var equipeService = new EquipeService(
            _predioUsuarioRepository.Object,
            _conviteFuncionarioRepository.Object,
            _usuarioRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _emailSender.Object,
            _conviteLinkBuilder.Object,
            autorizacaoPredioService);

        _sut = new ConviteController(equipeService, new AceitarConviteDtoValidator());

        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash-calculado");
    }

    [Fact]
    public async Task ObterDetalhe_com_token_valido_retorna_200_com_os_dados_do_convite()
    {
        var convite = CriarConvite();
        typeof(ConviteFuncionario).GetProperty(nameof(ConviteFuncionario.Predio))!
            .SetValue(convite, new Predio("Edifício Aurora", "Rua X, 100", SindicoId));

        _conviteFuncionarioRepository
            .Setup(r => r.BuscarPorHashAsync("hash-calculado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("ana@eusindico.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await _sut.ObterDetalhe("token", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<ConviteDetalheDto>(ok.Value);
        Assert.Equal("Edifício Aurora", corpo.PredioNome);
        Assert.False(corpo.ContaJaExiste);
    }

    [Fact]
    public async Task Aceitar_com_nome_invalido_retorna_400()
    {
        var resultado = await _sut.Aceitar(
            "token", new AceitarConviteDto("<script>alert('Pedro')</script>", "Senha@123"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Aceitar_com_dados_validos_retorna_201()
    {
        var convite = CriarConvite();
        _conviteFuncionarioRepository
            .Setup(r => r.BuscarPorHashAsync("hash-calculado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("ana@eusindico.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-novo");

        var resultado = await _sut.Aceitar("token", new AceitarConviteDto("Ana Souza", "Senha@123"), CancellationToken.None);

        var objeto = Assert.IsType<StatusCodeResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, objeto.StatusCode);
    }

    private static ConviteFuncionario CriarConvite() =>
        new(PredioId, "ana@eusindico.com", PapelPredio.Gestor, "hash-calculado", SindicoId, DateTime.UtcNow.AddDays(7));
}
