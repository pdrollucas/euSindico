using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Documentos;
using euSindico.Application.Documentos.Dtos;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa <see cref="AtaController"/> e <see cref="NormaController"/> (que compartilham o comportamento de
/// <see cref="DocumentoControllerBase"/>) com um <see cref="DocumentoService"/> real e as dependências
/// mockadas (mesmo padrão de RelatorioControllerTests).
/// </summary>
public class DocumentoControllerTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;

    private static readonly byte[] Pdf = "%PDF-1.4\n%teste"u8.ToArray();

    private readonly Mock<IDocumentoRepository> _documentoRepository = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly DocumentoService _service;

    public DocumentoControllerTests()
    {
        _service = new DocumentoService(
            _documentoRepository.Object,
            _storage.Object,
            new AutorizacaoPredioService(_predioUsuarioRepository.Object),
            TimeProvider.System,
            NullLogger<DocumentoService>.Instance);

        var vinculo = PredioUsuario.CriarComoDono(PredioId, UsuarioId);
        Definir(vinculo, nameof(PredioUsuario.Predio), new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
    }

    public static TheoryData<string> Tipos => new() { "ata", "norma" };

    private DocumentoControllerBase Criar(string tipo)
    {
        DocumentoControllerBase controller = tipo == "ata" ? new AtaController(_service) : new NormaController(_service);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioId.ToString()) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) },
        };
        return controller;
    }

    private static TipoDocumentoPredio TipoEsperado(string tipo) =>
        tipo == "ata" ? TipoDocumentoPredio.Ata : TipoDocumentoPredio.Norma;

    [Fact]
    public void Ata_e_Norma_tem_rotas_distintas_aninhadas_no_predio()
    {
        static string Rota(Type tipo) =>
            ((RouteAttribute)Attribute.GetCustomAttribute(tipo, typeof(RouteAttribute))!).Template;

        Assert.Equal("predios/{predioId:int}/atas", Rota(typeof(AtaController)));
        Assert.Equal("predios/{predioId:int}/normas", Rota(typeof(NormaController)));
    }

    // ---------- Enviar ----------

    [Theory]
    [MemberData(nameof(Tipos))]
    public async Task Enviar_com_arquivo_valido_retorna_201_com_location_e_grava_o_tipo_da_rota(string tipo)
    {
        var sut = Criar(tipo);

        var resultado = await sut.Enviar(PredioId, Arquivo("ata.pdf", Pdf), CancellationToken.None);

        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(DocumentoControllerBase.Obter), criado.ActionName);
        Assert.Equal(PredioId, criado.RouteValues!["predioId"]);
        Assert.Equal("ata.pdf", Assert.IsType<DocumentoDto>(criado.Value).NomeArquivo);
        _documentoRepository.Verify(
            r => r.AdicionarAsync(
                It.Is<Documento>(d => d.TipoDocumentoId == (int)TipoEsperado(tipo)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _storage.Verify(
            s => s.SalvarAsync(It.IsAny<string>(), Pdf, "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enviar_sem_arquivo_retorna_400()
    {
        var resultado = await Criar("ata").Enviar(PredioId, null, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Enviar_com_arquivo_vazio_retorna_400()
    {
        var resultado = await Criar("ata").Enviar(PredioId, Arquivo("ata.pdf", []), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Enviar_acima_do_limite_retorna_400_sem_consultar_nada()
    {
        var grande = new FormFile(Stream.Null, 0, ArquivoDocumento.TamanhoMaximoBytes + 1, "arquivo", "grande.pdf");

        var resultado = await Criar("ata").Enviar(PredioId, grande, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _predioUsuarioRepository.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Enviar_com_formato_invalido_propaga_ArquivoDocumentoInvalidoException()
    {
        await Assert.ThrowsAsync<ArquivoDocumentoInvalidoException>(
            () => Criar("ata").Enviar(PredioId, Arquivo("virus.exe", Pdf), CancellationToken.None));
    }

    // ---------- Listar ----------

    [Theory]
    [MemberData(nameof(Tipos))]
    public async Task Listar_com_defaults_filtra_pelo_tipo_da_rota_pagina_1_e_tamanho_10(string tipo)
    {
        _documentoRepository
            .Setup(r => r.ListarAsync(PredioId, TipoEsperado(tipo), null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Documento>(), 0));

        var resultado = await Criar(tipo).Listar(PredioId);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.IsType<PagedResultDto<DocumentoDto>>(ok.Value);
    }

    [Fact]
    public async Task Listar_repassa_a_busca_por_nome()
    {
        _documentoRepository
            .Setup(r => r.ListarAsync(PredioId, TipoDocumentoPredio.Ata, "assembleia", 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Documento>(), 0));

        await Criar("ata").Listar(PredioId, nome: "assembleia", page: 2, pageSize: 5);

        _documentoRepository.Verify(
            r => r.ListarAsync(PredioId, TipoDocumentoPredio.Ata, "assembleia", 2, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, 21, null)]
    public async Task Listar_com_paginacao_invalida_retorna_400(int page, int pageSize, string? nome)
    {
        var resultado = await Criar("ata").Listar(PredioId, nome, page, pageSize);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_busca_longa_demais_retorna_400()
    {
        var resultado = await Criar("ata").Listar(PredioId, new string('a', 256));

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    // ---------- Obter / Baixar / Remover ----------

    [Fact]
    public async Task Obter_devolve_o_documento()
    {
        ConfigurarDocumento(TipoDocumentoPredio.Norma);

        var resultado = await Criar("norma").Obter(PredioId, 5, CancellationToken.None);

        Assert.Equal(5, Assert.IsType<DocumentoDto>(Assert.IsType<OkObjectResult>(resultado).Value).Id);
    }

    [Fact]
    public async Task Obter_de_ata_pela_rota_de_normas_propaga_DocumentoNaoEncontradoException()
    {
        ConfigurarDocumento(TipoDocumentoPredio.Ata);

        await Assert.ThrowsAsync<DocumentoNaoEncontradoException>(
            () => Criar("norma").Obter(PredioId, 5, CancellationToken.None));
    }

    [Fact]
    public async Task Baixar_devolve_o_arquivo_como_anexo_com_nome_content_type_e_nosniff()
    {
        ConfigurarDocumento(TipoDocumentoPredio.Ata);
        _storage
            .Setup(s => s.LerAsync("documentos/10/abc.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var sut = Criar("ata");

        var resultado = await sut.Baixar(PredioId, 5, CancellationToken.None);

        var arquivo = Assert.IsType<FileStreamResult>(resultado);
        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal("ata.pdf", arquivo.FileDownloadName);
        Assert.Equal("nosniff", sut.Response.Headers.XContentTypeOptions.ToString());
    }

    [Fact]
    public async Task Remover_retorna_204_e_apaga_o_documento()
    {
        var documento = ConfigurarDocumento(TipoDocumentoPredio.Norma);

        var resultado = await Criar("norma").Remover(PredioId, 5, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        _documentoRepository.Verify(r => r.RemoverAsync(documento, It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.RemoverAsync("documentos/10/abc.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Auxiliares ----------

    private Documento ConfigurarDocumento(TipoDocumentoPredio tipo)
    {
        var documento = new Documento("ata.pdf", "documentos/10/abc.pdf", (int)tipo, PredioId);
        Definir(documento, nameof(Documento.Id), 5);
        _documentoRepository.Setup(r => r.BuscarPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(documento);
        return documento;
    }

    private static FormFile Arquivo(string nome, byte[] conteudo) =>
        new(new MemoryStream(conteudo), 0, conteudo.Length, "arquivo", nome);

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);
}
