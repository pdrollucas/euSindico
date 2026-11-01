using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Application.Tests.Auth;

public class PerfilServiceTrocaEmailTests
{
    private const int UsuarioId = 1;

    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<ICodigoTrocaEmailRepository> _codigoRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Usuario _usuario = new("João Silva", "joao@eusindico.com", "hash-senha");
    private readonly PerfilService _sut;

    public PerfilServiceTrocaEmailTests()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(UsuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(_usuario);
        _passwordHasher.Setup(h => h.Verificar("Senha@123", "hash-senha")).Returns(true);
        _tokenService.Setup(t => t.GerarCodigoRedefinicaoSenha()).Returns(new CodigoRedefinicaoSenhaGerado("ABC234", "hash-codigo"));
        _tokenService.Setup(t => t.HashCodigoRedefinicaoSenha("ABC234")).Returns("hash-codigo");

        _sut = new PerfilService(
            _usuarioRepository.Object,
            new Mock<IRefreshTokenRepository>().Object,
            _codigoRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _emailSender.Object,
            new Mock<IFileStorageService>().Object,
            NullLogger<PerfilService>.Instance);
    }

    private static SolicitarTrocaEmailDto Pedido(string senha = "Senha@123") => new("novo@eusindico.com", senha);

    private static CodigoTrocaEmail CodigoValido(string novoEmail = "novo@eusindico.com") =>
        new(UsuarioId, novoEmail, "hash-codigo", DateTime.UtcNow.AddMinutes(10));

    [Fact]
    public async Task Solicitar_com_dados_validos_grava_o_codigo_e_envia_para_o_novo_email()
    {
        await _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido());

        _codigoRepository.Verify(r => r.InvalidarTodosDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
        _codigoRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<CodigoTrocaEmail>(c => c.UsuarioId == UsuarioId && c.NovoEmail == "novo@eusindico.com" && c.CodigoHash == "hash-codigo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSender.Verify(
            e => e.EnviarAsync(
                "novo@eusindico.com",
                It.IsAny<string>(),
                It.Is<string>(corpo => corpo.Contains("ABC234")),
                It.IsAny<CancellationToken>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task Solicitar_nao_altera_o_email_antes_da_confirmacao()
    {
        await _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido());

        Assert.Equal("joao@eusindico.com", _usuario.Email);
        _usuarioRepository.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Solicitar_com_senha_incorreta_lanca_excecao_e_nao_envia_nada()
    {
        await Assert.ThrowsAsync<SenhaAtualIncorretaException>(() => _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido("errada")));

        _codigoRepository.Verify(r => r.AdicionarAsync(It.IsAny<CodigoTrocaEmail>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Solicitar_com_email_ja_cadastrado_lanca_excecao_e_nao_envia_nada()
    {
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("novo@eusindico.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<EmailJaCadastradoException>(() => _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido()));

        _codigoRepository.Verify(r => r.AdicionarAsync(It.IsAny<CodigoTrocaEmail>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Solicitar_dentro_do_cooldown_lanca_excecao_e_nao_envia_nada()
    {
        _codigoRepository
            .Setup(r => r.BuscarUltimoDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CodigoValido());

        await Assert.ThrowsAsync<SolicitacaoCodigoRecenteException>(() => _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido()));

        _codigoRepository.Verify(r => r.AdicionarAsync(It.IsAny<CodigoTrocaEmail>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Solicitar_apos_o_cooldown_gera_novo_codigo()
    {
        var antigo = CodigoValido();
        typeof(CodigoTrocaEmail).GetProperty(nameof(CodigoTrocaEmail.CriadoEm))!.SetValue(antigo, DateTime.UtcNow.AddMinutes(-3));
        _codigoRepository.Setup(r => r.BuscarUltimoDoUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(antigo);

        await _sut.SolicitarTrocaEmailAsync(UsuarioId, Pedido());

        _codigoRepository.Verify(r => r.AdicionarAsync(It.IsAny<CodigoTrocaEmail>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Solicitar_com_usuario_inexistente_lanca_excecao()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => _sut.SolicitarTrocaEmailAsync(99, Pedido()));
    }

    [Fact]
    public async Task Confirmar_com_codigo_valido_troca_o_email_e_marca_o_codigo_como_usado()
    {
        var codigo = CodigoValido();
        _codigoRepository.Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>())).ReturnsAsync(codigo);

        var resultado = await _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234"));

        Assert.Equal("novo@eusindico.com", resultado.Email);
        Assert.Equal("novo@eusindico.com", _usuario.Email);
        Assert.NotNull(codigo.UsadoEm);
        _codigoRepository.Verify(r => r.AtualizarAsync(codigo, It.IsAny<CancellationToken>()), Times.Once);
        _usuarioRepository.Verify(r => r.AtualizarAsync(_usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirmar_avisa_o_email_antigo()
    {
        _codigoRepository
            .Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CodigoValido());

        await _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234"));

        _emailSender.Verify(
            e => e.EnviarAsync("joao@eusindico.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null),
            Times.Once);
    }

    [Fact]
    public async Task Confirmar_se_o_aviso_ao_email_antigo_falha_a_troca_continua_valendo()
    {
        _codigoRepository
            .Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CodigoValido());
        _emailSender
            .Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new InvalidOperationException("smtp fora"));

        await _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234"));

        Assert.Equal("novo@eusindico.com", _usuario.Email);
    }

    [Fact]
    public async Task Confirmar_com_codigo_inexistente_lanca_excecao_e_nao_troca()
    {
        await Assert.ThrowsAsync<CodigoRedefinicaoInvalidoException>(
            () => _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234")));

        Assert.Equal("joao@eusindico.com", _usuario.Email);
    }

    [Fact]
    public async Task Confirmar_com_codigo_expirado_lanca_excecao_e_nao_troca()
    {
        var expirado = new CodigoTrocaEmail(UsuarioId, "novo@eusindico.com", "hash-codigo", DateTime.UtcNow.AddMinutes(-1));
        _codigoRepository.Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>())).ReturnsAsync(expirado);

        await Assert.ThrowsAsync<CodigoRedefinicaoInvalidoException>(
            () => _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234")));

        Assert.Equal("joao@eusindico.com", _usuario.Email);
    }

    [Fact]
    public async Task Confirmar_com_codigo_ja_usado_lanca_excecao_e_nao_troca()
    {
        var usado = CodigoValido();
        usado.MarcarComoUsado();
        _codigoRepository.Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>())).ReturnsAsync(usado);

        await Assert.ThrowsAsync<CodigoRedefinicaoInvalidoException>(
            () => _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234")));

        Assert.Equal("joao@eusindico.com", _usuario.Email);
    }

    [Fact]
    public async Task Confirmar_quando_o_email_foi_ocupado_depois_da_solicitacao_lanca_excecao_e_nao_troca()
    {
        var codigo = CodigoValido();
        _codigoRepository.Setup(r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>())).ReturnsAsync(codigo);
        _usuarioRepository.Setup(r => r.ExisteEmailAsync("novo@eusindico.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<EmailJaCadastradoException>(
            () => _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234")));

        Assert.Equal("joao@eusindico.com", _usuario.Email);
        Assert.Null(codigo.UsadoEm);
    }

    [Fact]
    public async Task Confirmar_consulta_o_codigo_sempre_com_o_id_do_usuario_autenticado()
    {
        await Assert.ThrowsAsync<CodigoRedefinicaoInvalidoException>(
            () => _sut.ConfirmarTrocaEmailAsync(UsuarioId, new ConfirmarTrocaEmailDto("ABC234")));

        _codigoRepository.Verify(
            r => r.BuscarPorUsuarioIdEHashAsync(UsuarioId, "hash-codigo", It.IsAny<CancellationToken>()), Times.Once);
        _codigoRepository.Verify(
            r => r.BuscarPorUsuarioIdEHashAsync(It.Is<int>(id => id != UsuarioId), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
