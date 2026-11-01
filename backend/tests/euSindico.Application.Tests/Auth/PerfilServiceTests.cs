using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Application.Tests.Auth;

public class PerfilServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ICodigoTrocaEmailRepository> _codigoTrocaEmailRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly PerfilService _sut;

    private static readonly ExcluirContaDto SenhaCorreta = new("Senha@123");

    public PerfilServiceTests()
    {
        _usuarioRepository
            .Setup(r => r.ExcluirUsuarioEDadosRelacionadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _passwordHasher.Setup(h => h.Verificar(SenhaCorreta.Senha, "hash-fake")).Returns(true);

        _sut = new PerfilService(
            _usuarioRepository.Object,
            _refreshTokenRepository.Object,
            _codigoTrocaEmailRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _emailSender.Object,
            _fileStorage.Object,
            NullLogger<PerfilService>.Instance);
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
    public async Task AtualizarPerfilAsync_com_usuario_existente_atualiza_so_o_nome()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var dto = new AtualizarPerfilDto("João Souza");
        var resultado = await _sut.AtualizarPerfilAsync(1, dto);

        Assert.Equal("João Souza", resultado.Nome);
        Assert.Equal("joao@eusindico.com", resultado.Email);
        _usuarioRepository.Verify(
            r => r.AtualizarAsync(
                It.Is<Usuario>(u => u.Nome == "João Souza" && u.Email == "joao@eusindico.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AtualizarPerfilAsync_com_usuario_inexistente_lanca_excecao()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var dto = new AtualizarPerfilDto("João Souza");

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
    public async Task AlterarSenhaAsync_avisa_o_usuario_por_email()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-antigo");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("SenhaAtual@1", "hash-antigo")).Returns(true);
        _passwordHasher.Setup(h => h.Hash("SenhaNova@2")).Returns("hash-novo");

        await _sut.AlterarSenhaAsync(1, new AlterarSenhaDto("SenhaAtual@1", "SenhaNova@2"));

        _emailSender.Verify(
            e => e.EnviarAsync("joao@eusindico.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null),
            Times.Once);
    }

    [Fact]
    public async Task AlterarSenhaAsync_se_o_aviso_por_email_falha_a_troca_continua_valendo()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-antigo");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verificar("SenhaAtual@1", "hash-antigo")).Returns(true);
        _passwordHasher.Setup(h => h.Hash("SenhaNova@2")).Returns("hash-novo");
        _emailSender
            .Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new InvalidOperationException("smtp fora"));

        await _sut.AlterarSenhaAsync(1, new AlterarSenhaDto("SenhaAtual@1", "SenhaNova@2"));

        Assert.Equal("hash-novo", usuario.SenhaHash);
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

    [Fact]
    public async Task ExcluirContaAsync_com_usuario_existente_exclui_usuario_e_dados_relacionados()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _usuarioRepository.Verify(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirContaAsync_com_senha_incorreta_lanca_excecao_e_nao_exclui_nada()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Assert.ThrowsAsync<SenhaAtualIncorretaException>(
            () => _sut.ExcluirContaAsync(1, new ExcluirContaDto("SenhaErrada")));

        _usuarioRepository.Verify(r => r.ExcluirUsuarioEDadosRelacionadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _fileStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExcluirContaAsync_avisa_o_email_da_conta_excluida()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _emailSender.Verify(
            e => e.EnviarAsync("joao@eusindico.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null),
            Times.Once);
    }

    [Fact]
    public async Task ExcluirContaAsync_se_o_aviso_por_email_falha_a_exclusao_conclui()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _emailSender
            .Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new InvalidOperationException("smtp fora"));

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _usuarioRepository.Verify(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirContaAsync_de_Sindico_limpa_o_prefixo_de_cada_predio_apagado_no_armazenamento()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([10, 20]);

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _fileStorage.Verify(s => s.RemoverPrefixoAsync("relatorios/10/", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.Verify(s => s.RemoverPrefixoAsync("relatorios/20/", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.Verify(s => s.RemoverPrefixoAsync("documentos/10/", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.Verify(s => s.RemoverPrefixoAsync("documentos/20/", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExcluirContaAsync_de_funcionario_nao_toca_no_armazenamento()
    {
        var usuario = new Usuario("Carlos", "carlos@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _fileStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExcluirContaAsync_se_o_banco_falha_nao_apaga_nada_no_armazenamento()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("banco fora"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExcluirContaAsync(1, SenhaCorreta));

        _fileStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExcluirContaAsync_se_o_armazenamento_falha_ainda_conclui_e_tenta_os_demais_predios()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([10, 20]);
        _fileStorage
            .Setup(s => s.RemoverPrefixoAsync("relatorios/10/", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("s3 fora"));

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _fileStorage.Verify(s => s.RemoverPrefixoAsync("relatorios/20/", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirContaAsync_se_a_limpeza_dos_relatorios_falha_ainda_limpa_os_documentos_do_mesmo_predio()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ExcluirUsuarioEDadosRelacionadosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([10]);
        _fileStorage
            .Setup(s => s.RemoverPrefixoAsync("relatorios/10/", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("s3 fora"));

        await _sut.ExcluirContaAsync(1, SenhaCorreta);

        _fileStorage.Verify(s => s.RemoverPrefixoAsync("documentos/10/", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirContaAsync_com_usuario_inexistente_lanca_excecao_e_nao_exclui()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => _sut.ExcluirContaAsync(99, SenhaCorreta));

        _usuarioRepository.Verify(r => r.ExcluirUsuarioEDadosRelacionadosAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
