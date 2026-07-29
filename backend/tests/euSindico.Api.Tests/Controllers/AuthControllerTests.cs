using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa a lógica do cookie HttpOnly do refresh token no <see cref="AuthController"/> —
/// leitura/escrita/limpeza de cookie, que não é coberta pelos testes de <c>AuthService</c>
/// (que não conhece HTTP, ver ARCHITECTURE.md). Usa um <see cref="AuthService"/> real com
/// repositórios mockados (mesmo padrão de AuthServiceTests), sem precisar de banco — a
/// asserção é sobre o header "Set-Cookie" da resposta, não sobre regra de negócio.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ICodigoRedefinicaoSenhaRepository> _codigoRedefinicaoSenhaRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        var authService = new AuthService(
            _usuarioRepository.Object,
            _refreshTokenRepository.Object,
            _codigoRedefinicaoSenhaRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _emailSender.Object);

        _sut = new AuthController(
            authService,
            new RegistrarUsuarioDtoValidator(),
            new LoginDtoValidator(),
            new EsqueciSenhaDtoValidator(),
            new VerificarCodigoDtoValidator(),
            new RedefinirSenhaDtoValidator())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task Login_com_credenciais_validas_define_cookie_httponly_e_devolve_so_o_access_token_no_corpo()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-armazenado");
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("Senha@123", "hash-armazenado")).Returns(true);
        _tokenService.Setup(t => t.GerarAccessToken(usuario)).Returns("access-token-fake");
        _tokenService.Setup(t => t.GerarRefreshToken())
            .Returns(new RefreshTokenGerado("refresh-token-fake", "hash-do-refresh"));

        var resultado = await _sut.Login(new LoginDto("joao@eusindico.com", "Senha@123"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<AccessTokenResponseDto>(ok.Value);
        Assert.Equal("access-token-fake", corpo.AccessToken);

        var setCookie = Assert.Single(_sut.Response.Headers.SetCookie);
        Assert.Contains("refreshToken=refresh-token-fake", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/auth", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_sem_cookie_lanca_refresh_token_invalido_sem_consultar_o_banco()
    {
        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(() => _sut.Refresh(CancellationToken.None));

        _refreshTokenRepository.Verify(r => r.BuscarPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_com_cookie_valido_rotaciona_e_define_o_novo_cookie()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-armazenado");
        var refreshTokenAtual = new RefreshToken(usuario.Id, "hash-antigo", DateTime.UtcNow.AddHours(3));
        _tokenService.Setup(t => t.HashRefreshToken("refresh-antigo")).Returns("hash-antigo");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-antigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenAtual);
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(usuario.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _tokenService.Setup(t => t.GerarAccessToken(usuario)).Returns("novo-access-token");
        _tokenService.Setup(t => t.GerarRefreshToken())
            .Returns(new RefreshTokenGerado("novo-refresh-token", "novo-hash"));
        DefinirCookieNaRequisicao("refreshToken", "refresh-antigo");

        var resultado = await _sut.Refresh(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var corpo = Assert.IsType<AccessTokenResponseDto>(ok.Value);
        Assert.Equal("novo-access-token", corpo.AccessToken);

        var setCookie = Assert.Single(_sut.Response.Headers.SetCookie);
        Assert.Contains("refreshToken=novo-refresh-token", setCookie);
        Assert.False(refreshTokenAtual.EstaAtivo);
    }

    [Fact]
    public async Task Logout_sem_cookie_nao_consulta_o_banco_mas_limpa_o_cookie_mesmo_assim()
    {
        DefinirUsuarioAutenticado(1);

        var resultado = await _sut.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _refreshTokenRepository.Verify(r => r.BuscarPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        var setCookie = Assert.Single(_sut.Response.Headers.SetCookie);
        Assert.StartsWith("refreshToken=", setCookie);
    }

    [Fact]
    public async Task Logout_com_cookie_valido_revoga_no_banco_e_limpa_o_cookie()
    {
        DefinirUsuarioAutenticado(1);
        var refreshToken = new RefreshToken(1, "hash-ativo", DateTime.UtcNow.AddHours(3));
        _tokenService.Setup(t => t.HashRefreshToken("token-valido")).Returns("hash-ativo");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-ativo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        DefinirCookieNaRequisicao("refreshToken", "token-valido");

        var resultado = await _sut.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(refreshToken.EstaAtivo);
        _refreshTokenRepository.Verify(r => r.AtualizarAsync(refreshToken, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_sut.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task Registrar_com_dados_validos_retorna_201_com_o_usuario()
    {
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-armazenado");

        var resultado = await _sut.Registrar(
            new RegistrarUsuarioDto("João Silva", "joao@eusindico.com", "Senha@123"), CancellationToken.None);

        var objeto = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, objeto.StatusCode);
        Assert.IsType<UsuarioDto>(objeto.Value);
    }

    [Fact]
    public async Task Registrar_com_email_invalido_retorna_400_sem_chamar_o_service()
    {
        var resultado = await _sut.Registrar(
            new RegistrarUsuarioDto("João Silva", "email-invalido", "Senha@123"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _usuarioRepository.Verify(
            r => r.ExisteEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EsqueciSenha_com_email_valido_retorna_204()
    {
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var resultado = await _sut.EsqueciSenha(
            new EsqueciSenhaDto("joao@eusindico.com"), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task EsqueciSenha_com_email_invalido_retorna_400()
    {
        var resultado = await _sut.EsqueciSenha(new EsqueciSenhaDto("invalido"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task VerificarCodigo_com_codigo_valido_retorna_204()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash");
        var codigoValido = new CodigoRedefinicaoSenha(usuario.Id, "hash-do-codigo", DateTime.UtcNow.AddMinutes(15));
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _tokenService.Setup(t => t.HashCodigoRedefinicaoSenha("ABC123")).Returns("hash-do-codigo");
        _codigoRedefinicaoSenhaRepository
            .Setup(r => r.BuscarPorUsuarioIdEHashAsync(usuario.Id, "hash-do-codigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(codigoValido);

        var resultado = await _sut.VerificarCodigo(
            new VerificarCodigoDto("joao@eusindico.com", "ABC123"), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task VerificarCodigo_com_codigo_de_tamanho_invalido_retorna_400()
    {
        var resultado = await _sut.VerificarCodigo(
            new VerificarCodigoDto("joao@eusindico.com", "123"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task RedefinirSenha_com_codigo_valido_retorna_204()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-antigo");
        var codigoValido = new CodigoRedefinicaoSenha(usuario.Id, "hash-do-codigo", DateTime.UtcNow.AddMinutes(15));
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _tokenService.Setup(t => t.HashCodigoRedefinicaoSenha("ABC123")).Returns("hash-do-codigo");
        _codigoRedefinicaoSenhaRepository
            .Setup(r => r.BuscarPorUsuarioIdEHashAsync(usuario.Id, "hash-do-codigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(codigoValido);
        _passwordHasher.Setup(h => h.Hash("NovaSenha@1")).Returns("hash-novo");

        var resultado = await _sut.RedefinirSenha(
            new RedefinirSenhaDto("joao@eusindico.com", "ABC123", "NovaSenha@1", "NovaSenha@1"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task RedefinirSenha_com_nova_senha_fraca_retorna_400()
    {
        var resultado = await _sut.RedefinirSenha(
            new RedefinirSenhaDto("joao@eusindico.com", "ABC123", "fraca", "fraca"), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    private void DefinirCookieNaRequisicao(string nome, string valor)
    {
        _sut.Request.Headers.Append("Cookie", $"{nome}={valor}");
    }

    private void DefinirUsuarioAutenticado(int usuarioId)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()) };
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
}
