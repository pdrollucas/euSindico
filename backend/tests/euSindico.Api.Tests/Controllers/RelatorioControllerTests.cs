using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Relatorios;
using euSindico.Application.Relatorios.Dtos;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="RelatorioController"/> com um <see cref="RelatorioService"/> real e as
/// dependências mockadas (mesmo padrão de PlanejamentoControllerTests).
/// </summary>
public class RelatorioControllerTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;

    private readonly Mock<IRelatorioRepository> _relatorioRepository = new();
    private readonly Mock<ICompromissoRepository> _compromissoRepository = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly RelatorioController _sut;

    public RelatorioControllerTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        var service = new RelatorioService(
            _relatorioRepository.Object,
            _compromissoRepository.Object,
            _storage.Object,
            new RelatorioPdfGenerator(),
            autorizacao,
            TimeProvider.System,
            NullLogger<RelatorioService>.Instance);

        var vinculo = PredioUsuario.CriarComoDono(PredioId, UsuarioId);
        Definir(vinculo, nameof(PredioUsuario.Predio), new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioId.ToString()) };
        _sut = new RelatorioController(service, new GerarRelatorioDtoValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) },
            },
        };
    }

    [Fact]
    public async Task Gerar_com_dados_validos_retorna_201_com_location_e_corpo()
    {
        var compromisso = new Compromisso("Visita", new DateOnly(2025, 3, 5), new TimeOnly(9, 0), PredioId, UsuarioId);
        compromisso.Concluir();
        Definir(compromisso, nameof(Compromisso.Responsavel), new Usuario("Maria", "m@x.com", "hash"));
        _compromissoRepository
            .Setup(r => r.ListarConcluidosDoMesAsync(PredioId, 2025, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([compromisso]);

        var resultado = await _sut.Gerar(PredioId, new GerarRelatorioDto(3, 2025), CancellationToken.None);

        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(RelatorioController.Obter), criado.ActionName);
        Assert.Equal(PredioId, criado.RouteValues!["predioId"]);
        var corpo = Assert.IsType<RelatorioDto>(criado.Value);
        Assert.Equal(3, corpo.MesReferencia);
        Assert.Equal(2025, corpo.AnoReferencia);
    }

    [Fact]
    public async Task Gerar_com_mes_invalido_retorna_400_e_nao_consulta_nada()
    {
        var resultado = await _sut.Gerar(PredioId, new GerarRelatorioDto(13, 2025), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _compromissoRepository.Verify(
            r => r.ListarConcluidosDoMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Gerar_com_corpo_vazio_retorna_400()
    {
        var resultado = await _sut.Gerar(PredioId, new GerarRelatorioDto(null, null), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_defaults_chama_o_service_com_pagina_1_e_tamanho_10()
    {
        _relatorioRepository
            .Setup(r => r.ListarAsync(PredioId, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Relatorio>(), 0));

        var resultado = await _sut.Listar(PredioId);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.IsType<PagedResultDto<RelatorioDto>>(ok.Value);
    }

    [Fact]
    public async Task Listar_repassa_ano_e_mes()
    {
        _relatorioRepository
            .Setup(r => r.ListarAsync(PredioId, 2026, 9, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Relatorio>(), 0));

        await _sut.Listar(PredioId, ano: 2026, mes: 9);

        _relatorioRepository.Verify(r => r.ListarAsync(PredioId, 2026, 9, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 10, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 21, null, null)]
    [InlineData(1, 10, 13, null)]
    [InlineData(1, 10, 0, null)]
    [InlineData(1, 10, null, 0)]
    public async Task Listar_com_parametros_invalidos_retorna_400(int page, int pageSize, int? mes, int? ano)
    {
        var resultado = await _sut.Listar(PredioId, ano, mes, page, pageSize);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Baixar_devolve_o_pdf_com_o_nome_do_arquivo()
    {
        var relatorio = new Relatorio(9, 2026, "Relatorio_2026-09.pdf", "relatorios/10/abc.pdf", PredioId);
        Definir(relatorio, nameof(Relatorio.Id), 5);
        _relatorioRepository.Setup(r => r.BuscarPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(relatorio);
        _storage
            .Setup(s => s.LerAsync("relatorios/10/abc.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var resultado = await _sut.Baixar(PredioId, 5, CancellationToken.None);

        var arquivo = Assert.IsType<FileStreamResult>(resultado);
        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal("Relatorio_2026-09.pdf", arquivo.FileDownloadName);
    }

    [Fact]
    public async Task Remover_retorna_204_e_apaga_o_relatorio()
    {
        var relatorio = new Relatorio(9, 2026, "Relatorio_2026-09.pdf", "relatorios/10/abc.pdf", PredioId);
        _relatorioRepository.Setup(r => r.BuscarPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(relatorio);

        var resultado = await _sut.Remover(PredioId, 5, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _relatorioRepository.Verify(r => r.RemoverAsync(relatorio, It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.RemoverAsync("relatorios/10/abc.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);
}
