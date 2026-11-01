using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Suporte;
using euSindico.Domain.Entities;
using Moq;

namespace euSindico.Application.Tests.Suporte;

public class SuporteServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<ISuporteDestino> _destino = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly SuporteService _sut;

    public SuporteServiceTests()
    {
        _destino.SetupGet(d => d.EmailDestino).Returns("suporte@eusindico.com");
        _sut = new SuporteService(_usuarioRepository.Object, _destino.Object, _emailSender.Object);
    }

    [Fact]
    public async Task EnviarAsync_manda_para_o_destino_configurado_com_reply_to_do_usuario()
    {
        _usuarioRepository
            .Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario("João Silva", "joao@eusindico.com", "hash"));

        await _sut.EnviarAsync(1, new EnviarSuporteDto("Erro no relatório", "Não consigo gerar o relatório de março."));

        _emailSender.Verify(
            e => e.EnviarComRespostaParaAsync(
                "suporte@eusindico.com",
                "joao@eusindico.com",
                "João Silva",
                "[Suporte euSíndico] Erro no relatório",
                It.Is<string>(t => t.Contains("João Silva") && t.Contains("joao@eusindico.com") && t.Contains("relatório de março")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_codifica_o_html_da_mensagem_do_usuario()
    {
        _usuarioRepository
            .Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario("João Silva", "joao@eusindico.com", "hash"));

        await _sut.EnviarAsync(1, new EnviarSuporteDto("Assunto", "<script>alert(1)</script> olá"));

        _emailSender.Verify(
            e => e.EnviarComRespostaParaAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => !html.Contains("<script>") && html.Contains("&lt;script&gt;")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_com_usuario_inexistente_lanca_excecao_e_nao_envia()
    {
        _usuarioRepository.Setup(r => r.BuscarPorIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(
            () => _sut.EnviarAsync(99, new EnviarSuporteDto("Assunto", "Mensagem de teste")));

        _emailSender.VerifyNoOtherCalls();
    }
}
