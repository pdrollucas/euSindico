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

    [Fact]
    public async Task RenovarTokenAsync_com_refresh_token_ativo_revoga_o_antigo_e_emite_par_novo_com_mesma_expiracao()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-armazenado");
        var refreshTokenAntigo = new RefreshToken(usuario.Id, "hash-antigo", DateTime.UtcNow.AddHours(3));
        _tokenService.Setup(t => t.HashRefreshToken("refresh-antigo")).Returns("hash-antigo");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-antigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenAntigo);
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(usuario.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _tokenService.Setup(t => t.GerarAccessToken(usuario)).Returns("novo-access-token");
        _tokenService.Setup(t => t.GerarRefreshToken())
            .Returns(new RefreshTokenGerado("novo-refresh-token", "novo-hash"));

        var dto = new RefreshTokenDto("refresh-antigo");

        var resultado = await _sut.RenovarTokenAsync(dto);

        Assert.Equal("novo-access-token", resultado.AccessToken);
        Assert.Equal("novo-refresh-token", resultado.RefreshToken);
        Assert.False(refreshTokenAntigo.EstaAtivo);
        _refreshTokenRepository.Verify(r => r.AtualizarAsync(refreshTokenAntigo, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<RefreshToken>(rt => rt.TokenHash == "novo-hash" && rt.ExpiraEm == refreshTokenAntigo.ExpiraEm),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RenovarTokenAsync_com_hash_inexistente_lanca_refresh_token_invalido()
    {
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-desconhecido");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-desconhecido", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var dto = new RefreshTokenDto("token-qualquer");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(() => _sut.RenovarTokenAsync(dto));

        _refreshTokenRepository.Verify(r => r.AdicionarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RenovarTokenAsync_com_refresh_token_ja_revogado_lanca_refresh_token_invalido()
    {
        var refreshTokenRevogado = new RefreshToken(1, "hash-revogado", DateTime.UtcNow.AddHours(3));
        refreshTokenRevogado.Revogar();
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-revogado");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-revogado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenRevogado);

        var dto = new RefreshTokenDto("token-qualquer");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(() => _sut.RenovarTokenAsync(dto));
    }

    [Fact]
    public async Task RenovarTokenAsync_com_refresh_token_expirado_lanca_refresh_token_invalido()
    {
        var refreshTokenExpirado = new RefreshToken(1, "hash-expirado", DateTime.UtcNow.AddSeconds(-1));
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-expirado");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-expirado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenExpirado);

        var dto = new RefreshTokenDto("token-qualquer");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(() => _sut.RenovarTokenAsync(dto));
    }

    [Fact]
    public async Task LogoutAsync_com_refresh_token_ativo_do_proprio_usuario_revoga()
    {
        var refreshToken = new RefreshToken(1, "hash-ativo", DateTime.UtcNow.AddHours(3));
        _tokenService.Setup(t => t.HashRefreshToken("token-valido")).Returns("hash-ativo");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-ativo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var dto = new RefreshTokenDto("token-valido");

        await _sut.LogoutAsync(1, dto);

        Assert.False(refreshToken.EstaAtivo);
        _refreshTokenRepository.Verify(r => r.AtualizarAsync(refreshToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_com_hash_inexistente_nao_lanca_e_nao_persiste()
    {
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-desconhecido");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-desconhecido", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var dto = new RefreshTokenDto("token-qualquer");

        await _sut.LogoutAsync(1, dto);

        _refreshTokenRepository.Verify(r => r.AtualizarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_com_refresh_token_ja_revogado_nao_persiste_de_novo()
    {
        var refreshTokenRevogado = new RefreshToken(1, "hash-revogado", DateTime.UtcNow.AddHours(3));
        refreshTokenRevogado.Revogar();
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-revogado");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-revogado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenRevogado);

        var dto = new RefreshTokenDto("token-qualquer");

        await _sut.LogoutAsync(1, dto);

        _refreshTokenRepository.Verify(r => r.AtualizarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_com_refresh_token_de_outro_usuario_nao_revoga()
    {
        var refreshTokenDeOutroUsuario = new RefreshToken(2, "hash-de-outro", DateTime.UtcNow.AddHours(3));
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("hash-de-outro");
        _refreshTokenRepository.Setup(r => r.BuscarPorHashAsync("hash-de-outro", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenDeOutroUsuario);

        var dto = new RefreshTokenDto("token-qualquer");

        await _sut.LogoutAsync(1, dto);

        Assert.True(refreshTokenDeOutroUsuario.EstaAtivo);
        _refreshTokenRepository.Verify(r => r.AtualizarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
