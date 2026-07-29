using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="PerfilController"/> com um <see cref="PerfilService"/> real e repositórios
/// mockados (mesmo padrão de AuthControllerTests), sem precisar de banco. O id do usuário vem da
/// claim "sub" do token (RN02), então o HttpContext é montado com um usuário autenticado.
/// </summary>
public class PerfilControllerTests
{
    private const int UsuarioIdAutenticado = 1;

    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly PerfilController _sut;

    public PerfilControllerTests()
    {
        var perfilService = new PerfilService(
            _usuarioRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioIdAutenticado.ToString()) };
        _sut = new PerfilController(
            perfilService,
            new AtualizarPerfilDtoValidator(),
            new AlterarSenhaDtoValidator())
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
    public async Task ObterPerfil_retorna_200_com_os_dados_do_usuario()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var resultado = await _sut.ObterPerfil(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<UsuarioDto>(ok.Value);
        Assert.Equal("joao@eusindico.com", corpo.Email);
    }

    [Fact]
    public async Task AtualizarPerfil_com_dados_validos_retorna_200_e_persiste()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var resultado = await _sut.AtualizarPerfil(
            new AtualizarPerfilDto("João Souza", "joao.souza@eusindico.com"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.IsType<UsuarioDto>(ok.Value);
        _usuarioRepository.Verify(r => r.AtualizarAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarPerfil_com_email_invalido_retorna_400()
    {
        var resultado = await _sut.AtualizarPerfil(
            new AtualizarPerfilDto("João Souza", "email-invalido"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task AlterarSenha_com_senha_atual_correta_retorna_204_e_revoga_sessoes()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-atual");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("Senha@123", "hash-atual")).Returns(true);
        _passwordHasher.Setup(h => h.Hash("NovaSenha@1")).Returns("hash-novo");

        var resultado = await _sut.AlterarSenha(
            new AlterarSenhaDto("Senha@123", "NovaSenha@1"), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _refreshTokenRepository.Verify(
            r => r.RevogarTodosDoUsuarioAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlterarSenha_com_nova_senha_fraca_retorna_400()
    {
        var resultado = await _sut.AlterarSenha(
            new AlterarSenhaDto("Senha@123", "fraca"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task ExcluirConta_retorna_204_e_remove_usuario_e_dados_relacionados()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var resultado = await _sut.ExcluirConta(CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _usuarioRepository.Verify(
            r => r.ExcluirUsuarioEDadosRelacionadosAsync(UsuarioIdAutenticado, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
