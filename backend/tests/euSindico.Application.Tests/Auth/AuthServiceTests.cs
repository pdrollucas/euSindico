using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Moq;

namespace euSindico.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _usuarioRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object);
    }

    [Fact]
    public async Task RegistrarAsync_com_email_disponivel_cria_usuario_com_senha_hasheada()
    {
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-fake");

        var dto = new RegistrarUsuarioDto("João Silva", "joao@eusindico.com", "Senha@123");

        var resultado = await _sut.RegistrarAsync(dto);

        Assert.Equal("João Silva", resultado.Nome);
        Assert.Equal("joao@eusindico.com", resultado.Email);
        _usuarioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Usuario>(u => u.Email == "joao@eusindico.com" && u.SenhaHash == "hash-fake"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_com_email_ja_cadastrado_lanca_excecao_e_nao_persiste()
    {
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new RegistrarUsuarioDto("João Silva", "joao@eusindico.com", "Senha@123");

        await Assert.ThrowsAsync<EmailJaCadastradoException>(() => _sut.RegistrarAsync(dto));

        _usuarioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_com_credenciais_corretas_emite_access_e_refresh_token()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-armazenado");
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("Senha@123", "hash-armazenado")).Returns(true);
        _tokenService.Setup(t => t.GerarAccessToken(usuario)).Returns("access-token-fake");
        _tokenService.Setup(t => t.GerarRefreshToken())
            .Returns(new RefreshTokenGerado("refresh-token-fake", "hash-do-refresh"));

        var dto = new LoginDto("joao@eusindico.com", "Senha@123");

        var resultado = await _sut.LoginAsync(dto);

        Assert.Equal("access-token-fake", resultado.AccessToken);
        Assert.Equal("refresh-token-fake", resultado.RefreshToken);
        _refreshTokenRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<RefreshToken>(rt => rt.TokenHash == "hash-do-refresh" && rt.EstaAtivo),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_com_email_inexistente_lanca_credenciais_invalidas()
    {
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("naoexiste@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var dto = new LoginDto("naoexiste@eusindico.com", "Senha@123");

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() => _sut.LoginAsync(dto));

        _refreshTokenRepository.Verify(r => r.AdicionarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_com_senha_incorreta_lanca_credenciais_invalidas()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-armazenado");
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync("joao@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("SenhaErrada", "hash-armazenado")).Returns(false);

        var dto = new LoginDto("joao@eusindico.com", "SenhaErrada");

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() => _sut.LoginAsync(dto));

        _refreshTokenRepository.Verify(r => r.AdicionarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
