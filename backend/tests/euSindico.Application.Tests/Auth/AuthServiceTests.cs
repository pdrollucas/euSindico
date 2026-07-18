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
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_usuarioRepository.Object, _passwordHasher.Object);
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
}
