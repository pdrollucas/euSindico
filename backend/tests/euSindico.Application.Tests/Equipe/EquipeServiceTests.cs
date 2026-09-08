using System.Net;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Equipe.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Moq;

namespace euSindico.Application.Tests.Equipe;

public class EquipeServiceTests
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
    private readonly EquipeService _sut;

    public EquipeServiceTests()
    {
        var autorizacaoPredioService = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        _sut = new EquipeService(
            _predioUsuarioRepository.Object,
            _conviteFuncionarioRepository.Object,
            _usuarioRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _emailSender.Object,
            _conviteLinkBuilder.Object,
            autorizacaoPredioService);

        // Padrão: quem chama é o Síndico dono do prédio, e não há conflito de e-mail — os
        // testes que precisam de outro cenário sobrescrevem essas configurações. Usuario/Predio
        // são preenchidos porque ConvidarAsync os usa pra montar o e-mail de convite (nome do
        // síndico e do prédio), reaproveitando o mesmo vínculo já retornado pela autorização.
        var vinculoSindico = PredioUsuario.CriarComoDono(PredioId, SindicoId);
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Usuario))!
            .SetValue(vinculoSindico, new Usuario("João Silva", "joao@eusindico.com", "hash"));
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Predio))!
            .SetValue(vinculoSindico, new Predio("Edifício Aurora", "Rua X, 100", SindicoId));

        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, SindicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoSindico);
        _predioUsuarioRepository
            .Setup(r => r.ExisteMembroComEmailAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _conviteFuncionarioRepository
            .Setup(r => r.ExisteConvitePendenteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task ConvidarAsync_sem_ser_sindico_lanca_AcaoNaoPermitida_e_nao_cria_convite()
    {
        const int ajudanteId = 2;
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, ajudanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PredioUsuario.CriarComoConvidado(PredioId, ajudanteId, PapelPredio.Colaborador, SindicoId));

        var dto = new ConvidarFuncionarioDto("nova@eusindico.com", PapelPredio.Gestor);

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ConvidarAsync(ajudanteId, PredioId, dto));

        _conviteFuncionarioRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<ConviteFuncionario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConvidarAsync_com_email_ja_membro_lanca_EmailJaVinculado()
    {
        _predioUsuarioRepository
            .Setup(r => r.ExisteMembroComEmailAsync(PredioId, "ana@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Gestor);

        await Assert.ThrowsAsync<EmailJaVinculadoException>(() => _sut.ConvidarAsync(SindicoId, PredioId, dto));
    }

    [Fact]
    public async Task ConvidarAsync_com_convite_pendente_lanca_EmailJaVinculado()
    {
        _conviteFuncionarioRepository
            .Setup(r => r.ExisteConvitePendenteAsync(PredioId, "ana@eusindico.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Gestor);

        await Assert.ThrowsAsync<EmailJaVinculadoException>(() => _sut.ConvidarAsync(SindicoId, PredioId, dto));
    }

    [Fact]
    public async Task ConvidarAsync_com_dados_validos_cria_convite_e_envia_email_com_o_link()
    {
        _tokenService.Setup(t => t.GerarTokenConvite()).Returns(new ConviteTokenGerado("token-plano", "hash-token"));
        _conviteLinkBuilder.Setup(b => b.Construir("token-plano")).Returns("https://app.eusindico.com/convite/token-plano");

        var dto = new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Gestor);
        await _sut.ConvidarAsync(SindicoId, PredioId, dto);

        _conviteFuncionarioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<ConviteFuncionario>(c =>
                    c.Email == "ana@eusindico.com" && c.Papel == PapelPredio.Gestor && c.TokenHash == "hash-token"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailSender.Verify(
            e => e.EnviarAsync(
                "ana@eusindico.com",
                It.IsAny<string>(),
                It.Is<string>(corpo =>
                    corpo.Contains("https://app.eusindico.com/convite/token-plano") &&
                    corpo.Contains("João Silva") &&
                    corpo.Contains("Edifício Aurora")),
                It.IsAny<CancellationToken>(),
                It.Is<string?>(html =>
                    html != null &&
                    html.Contains("Aceitar convite") &&
                    html.Contains("https://app.eusindico.com/convite/token-plano") &&
                    html.Contains(WebUtility.HtmlEncode("João Silva")) &&
                    html.Contains(WebUtility.HtmlEncode("Edifício Aurora")))),
            Times.Once);
    }

    [Fact]
    public async Task ListarEquipeAsync_sem_ser_sindico_lanca_AcaoNaoPermitida()
    {
        const int ajudanteId = 2;
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, ajudanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PredioUsuario.CriarComoConvidado(PredioId, ajudanteId, PapelPredio.Colaborador, SindicoId));

        await Assert.ThrowsAsync<AcaoNaoPermitidaException>(() => _sut.ListarEquipeAsync(ajudanteId, PredioId));
    }

    [Fact]
    public async Task ListarEquipeAsync_retorna_membros_mapeados_com_dados_do_usuario()
    {
        var membro = PredioUsuario.CriarComoConvidado(PredioId, 2, PapelPredio.Colaborador, SindicoId);
        typeof(PredioUsuario).GetProperty(nameof(PredioUsuario.Usuario))!
            .SetValue(membro, new Usuario("Carlos Mendes", "carlos@eusindico.com", "hash"));

        _predioUsuarioRepository
            .Setup(r => r.ListarDoPredioAsync(PredioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membro]);

        var resultado = await _sut.ListarEquipeAsync(SindicoId, PredioId);

        var item = Assert.Single(resultado);
        Assert.Equal("Carlos Mendes", item.Nome);
        Assert.Equal("carlos@eusindico.com", item.Email);
        Assert.Equal(PapelPredio.Colaborador, item.Papel);
    }

    [Fact]
    public async Task RemoverAsync_sindico_tentando_remover_a_si_mesmo_lanca_RemoverDonoDoPredio()
    {
        await Assert.ThrowsAsync<RemoverDonoDoPredioException>(() => _sut.RemoverAsync(SindicoId, PredioId, SindicoId));

        _predioUsuarioRepository.Verify(
            r => r.RemoverAsync(It.IsAny<PredioUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_com_membro_inexistente_lanca_MembroNaoEncontrado()
    {
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredioUsuario?)null);

        await Assert.ThrowsAsync<MembroNaoEncontradoException>(() => _sut.RemoverAsync(SindicoId, PredioId, 99));
    }

    [Fact]
    public async Task RemoverAsync_com_membro_existente_remove_o_vinculo()
    {
        var vinculo = PredioUsuario.CriarComoConvidado(PredioId, 2, PapelPredio.Colaborador, SindicoId);
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        await _sut.RemoverAsync(SindicoId, PredioId, 2);

        _predioUsuarioRepository.Verify(r => r.RemoverAsync(vinculo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterDetalheConviteAsync_com_token_inexistente_lanca_ConviteInvalido()
    {
        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository
            .Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConviteFuncionario?)null);

        await Assert.ThrowsAsync<ConviteInvalidoException>(() => _sut.ObterDetalheConviteAsync("token"));
    }

    [Fact]
    public async Task ObterDetalheConviteAsync_com_token_expirado_lanca_ConviteInvalido()
    {
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-1));
        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository.Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(convite);

        await Assert.ThrowsAsync<ConviteInvalidoException>(() => _sut.ObterDetalheConviteAsync("token"));
    }

    [Fact]
    public async Task ObterDetalheConviteAsync_com_token_valido_retorna_detalhes_do_convite()
    {
        var convite = CriarConvite(DateTime.UtcNow.AddDays(7));
        typeof(ConviteFuncionario).GetProperty(nameof(ConviteFuncionario.Predio))!
            .SetValue(convite, new Predio("Edifício Aurora", "Rua X, 100", SindicoId));

        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository.Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.ExisteEmailAsync(convite.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var detalhe = await _sut.ObterDetalheConviteAsync("token");

        Assert.Equal("Edifício Aurora", detalhe.PredioNome);
        Assert.Equal(PapelPredio.Gestor, detalhe.Papel);
        Assert.False(detalhe.ContaJaExiste);
    }

    [Fact]
    public async Task AceitarConviteAsync_com_token_invalido_lanca_ConviteInvalido()
    {
        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository
            .Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConviteFuncionario?)null);

        await Assert.ThrowsAsync<ConviteInvalidoException>(
            () => _sut.AceitarConviteAsync("token", new AceitarConviteDto(null, null)));
    }

    [Fact]
    public async Task AceitarConviteAsync_sem_conta_existente_e_sem_nome_ou_senha_lanca_DadosCadastroObrigatorios()
    {
        var convite = CriarConvite(DateTime.UtcNow.AddDays(7));
        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository.Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync(convite.Email, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<DadosCadastroObrigatoriosException>(
            () => _sut.AceitarConviteAsync("token", new AceitarConviteDto(null, null)));

        _usuarioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AceitarConviteAsync_sem_conta_existente_cria_usuario_e_vinculo_e_marca_convite_usado()
    {
        var convite = CriarConvite(DateTime.UtcNow.AddDays(7));
        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository.Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync(convite.Email, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-novo");

        await _sut.AceitarConviteAsync("token", new AceitarConviteDto("Ana Souza", "Senha@123"));

        _usuarioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Usuario>(u => u.Nome == "Ana Souza" && u.Email == convite.Email && u.SenhaHash == "hash-novo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _predioUsuarioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<PredioUsuario>(v => v.PredioId == convite.PredioId && v.Papel == convite.Papel),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(convite.UsadoEm);
        _conviteFuncionarioRepository.Verify(r => r.AtualizarAsync(convite, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AceitarConviteAsync_com_conta_existente_ignora_nome_e_senha_e_so_cria_vinculo()
    {
        var convite = CriarConvite(DateTime.UtcNow.AddDays(7));
        var usuarioExistente = new Usuario("Ana Souza", convite.Email, "hash-existente");

        _tokenService.Setup(t => t.HashTokenConvite("token")).Returns("hash");
        _conviteFuncionarioRepository.Setup(r => r.BuscarPorHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(convite);
        _usuarioRepository.Setup(r => r.BuscarPorEmailAsync(convite.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuarioExistente);

        await _sut.AceitarConviteAsync("token", new AceitarConviteDto(null, null));

        _usuarioRepository.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _predioUsuarioRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<PredioUsuario>(v => v.PredioId == convite.PredioId && v.Papel == convite.Papel),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ConviteFuncionario CriarConvite(DateTime expiraEm) =>
        new(PredioId, "ana@eusindico.com", PapelPredio.Gestor, "hash-token", SindicoId, expiraEm);
}
