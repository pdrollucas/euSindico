using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
/// Testa o <see cref="EquipeController"/> com um <see cref="EquipeService"/> real e
/// repositórios mockados (mesmo padrão de PerfilControllerTests). O id do usuário vem da
/// claim "sub" do token (RN02) — aqui sempre o Síndico dono do prédio.
/// </summary>
public class EquipeControllerTests
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
    private readonly EquipeController _sut;

    public EquipeControllerTests()
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

        // Usuario/Predio precisam estar preenchidos porque ConvidarAsync os usa pra montar o
        // e-mail de convite (nome do síndico e do prédio) a partir do mesmo vínculo retornado
        // pela autorização.
        var vinculoSindico = PredioUsuario.CriarComoDono(PredioId, SindicoId);
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Usuario))!
            .SetValue(vinculoSindico, new Usuario("João Silva", "joao@eusindico.com", "hash"));
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Predio))!
            .SetValue(vinculoSindico, new Predio("Edifício Aurora", "Rua X, 100", SindicoId));

        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, SindicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoSindico);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, SindicoId.ToString()) };
        _sut = new EquipeController(equipeService, new ConvidarFuncionarioDtoValidator())
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
    public async Task Convidar_com_dados_validos_retorna_201()
    {
        _tokenService.Setup(t => t.GerarTokenConvite()).Returns(new ConviteTokenGerado("token", "hash"));
        _conviteLinkBuilder.Setup(b => b.Construir("token")).Returns("https://app.eusindico.com/convite/token");

        var resultado = await _sut.Convidar(
            PredioId, new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Gestor), CancellationToken.None);

        var objeto = Assert.IsType<StatusCodeResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, objeto.StatusCode);
    }

    [Fact]
    public async Task Convidar_com_papel_sindico_retorna_400()
    {
        var resultado = await _sut.Convidar(
            PredioId, new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Sindico), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task ListarEquipe_retorna_200_com_os_membros()
    {
        var membro = PredioUsuario.CriarComoConvidado(PredioId, 2, PapelPredio.Colaborador, SindicoId);
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Usuario))!
            .SetValue(membro, new Usuario("Carlos Mendes", "carlos@eusindico.com", "hash"));
        _predioUsuarioRepository
            .Setup(r => r.ListarDoPredioAsync(PredioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membro]);

        var resultado = await _sut.ListarEquipe(PredioId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<List<EquipeMembroDto>>(ok.Value);
        Assert.Single(corpo);
    }

    [Fact]
    public async Task RemoverMembro_com_membro_existente_retorna_204()
    {
        var vinculo = PredioUsuario.CriarComoConvidado(PredioId, 2, PapelPredio.Colaborador, SindicoId);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var resultado = await _sut.RemoverMembro(PredioId, 2, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }
}
