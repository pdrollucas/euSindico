using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Moq;

namespace euSindico.Application.Tests.Auth;

public class PerfilServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly PerfilService _sut;

    public PerfilServiceTests()
    {
        _sut = new PerfilService(_usuarioRepository.Object, _refreshTokenRepository.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task ObterPerfilAsync_com_usuario_existente_retorna_dados()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var resultado = await _sut.ObterPerfilAsync(1);

        Assert.Equal("João Silva", resultado.Nome);
        Assert.Equal("joao@eusindico.com", resultado.Email);
    }

    [Fact]
    public async Task ObterPerfilAsync_com_usuario_inexistente_lanca_excecao()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => _sut.ObterPerfilAsync(99));
    }

    [Fact]
    public async Task AtualizarPerfilAsync_com_usuario_existente_atualiza_nome_e_email()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var dto = new AtualizarPerfilDto("João Souza", "joao.souza@eusindico.com");
        var resultado = await _sut.AtualizarPerfilAsync(1, dto);

        Assert.Equal("João Souza", resultado.Nome);
        Assert.Equal("joao.souza@eusindico.com", resultado.Email);
        _usuarioRepository.Verify(
            r => r.AtualizarAsync(
                It.Is<Usuario>(u => u.Nome == "João Souza" && u.Email == "joao.souza@eusindico.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AtualizarPerfilAsync_com_usuario_inexistente_lanca_excecao()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var dto = new AtualizarPerfilDto("João Souza", "joao.souza@eusindico.com");

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => _sut.AtualizarPerfilAsync(99, dto));
    }

    [Fact]
    public async Task AlterarSenhaAsync_com_senha_atual_correta_atualiza_hash_e_revoga_sessoes()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-antigo");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("SenhaAtual@1", "hash-antigo")).Returns(true);
        _passwordHasher.Setup(h => h.Hash("SenhaNova@2")).Returns("hash-novo");

        var dto = new AlterarSenhaDto("SenhaAtual@1", "SenhaNova@2");
        await _sut.AlterarSenhaAsync(1, dto);

        Assert.Equal("hash-novo", usuario.SenhaHash);
        _usuarioRepository.Verify(r => r.AtualizarAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevogarTodosDoUsuarioAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlterarSenhaAsync_com_senha_atual_incorreta_lanca_excecao_e_nao_altera_nada()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-antigo");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("SenhaErrada", "hash-antigo")).Returns(false);

        var dto = new AlterarSenhaDto("SenhaErrada", "SenhaNova@2");

        await Assert.ThrowsAsync<SenhaAtualIncorretaException>(() => _sut.AlterarSenhaAsync(1, dto));

        Assert.Equal("hash-antigo", usuario.SenhaHash);
        _usuarioRepository.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.RevogarTodosDoUsuarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AlterarSenhaAsync_com_usuario_inexistente_lanca_excecao()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var dto = new AlterarSenhaDto("SenhaAtual@1", "SenhaNova@2");

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => _sut.AlterarSenhaAsync(99, dto));
    }
}
