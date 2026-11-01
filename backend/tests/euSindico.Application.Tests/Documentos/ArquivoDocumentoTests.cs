using euSindico.Application.Common.Exceptions;
using euSindico.Application.Documentos;

namespace euSindico.Application.Tests.Documentos;

public class ArquivoDocumentoTests
{
    [Fact]
    public void Validar_pdf_devolve_content_type_e_extensao()
    {
        var resultado = ArquivoDocumento.Validar("Ata da assembleia.pdf", ArquivosDeTeste.Pdf());

        Assert.Equal("Ata da assembleia.pdf", resultado.NomeArquivo);
        Assert.Equal(".pdf", resultado.Extensao);
        Assert.Equal("application/pdf", resultado.ContentType);
    }

    [Fact]
    public void Validar_docx_exige_a_estrutura_do_word()
    {
        var docx = ArquivoDocumento.Validar("regimento.docx", ArquivosDeTeste.Docx());

        Assert.StartsWith("application/vnd.openxmlformats-officedocument.wordprocessingml", docx.ContentType);
    }

    [Theory]
    [InlineData("foto.jpg", "image/jpeg")]
    [InlineData("foto.jpeg", "image/jpeg")]
    [InlineData("FOTO.JPG", "image/jpeg")]
    [InlineData("imagem.png", "image/png")]
    public void Validar_imagens_aceita_jpg_e_png_sem_diferenciar_maiusculas(string nome, string contentType)
    {
        var conteudo = contentType == "image/png" ? ArquivosDeTeste.Png() : ArquivosDeTeste.Jpeg();

        Assert.Equal(contentType, ArquivoDocumento.Validar(nome, conteudo).ContentType);
    }

    [Fact]
    public void Validar_apara_espacos_do_nome()
    {
        Assert.Equal("ata.pdf", ArquivoDocumento.Validar("  ata.pdf  ", ArquivosDeTeste.Pdf()).NomeArquivo);
    }

    [Theory]
    [InlineData("script.exe")]
    [InlineData("pagina.html")]
    [InlineData("texto.txt")]
    [InlineData("antigo.doc")]
    [InlineData("planilha.xlsx")]
    [InlineData("semextensao")]
    public void Validar_formato_fora_de_RN12_lanca_excecao(string nome)
    {
        var ex = Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar(nome, ArquivosDeTeste.Pdf()));

        Assert.Equal(ArquivoDocumento.MensagemFormato, ex.Message);
    }

    [Fact]
    public void Validar_arquivo_acima_de_20_MB_lanca_excecao()
    {
        var conteudo = new byte[ArquivoDocumento.TamanhoMaximoBytes + 1];
        ArquivosDeTeste.Pdf().CopyTo(conteudo, 0);

        var ex = Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar("grande.pdf", conteudo));

        Assert.Equal(ArquivoDocumento.MensagemTamanho, ex.Message);
    }

    [Fact]
    public void Validar_arquivo_de_exatamente_20_MB_e_aceito()
    {
        var conteudo = new byte[ArquivoDocumento.TamanhoMaximoBytes];
        ArquivosDeTeste.Pdf().CopyTo(conteudo, 0);

        Assert.Equal(".pdf", ArquivoDocumento.Validar("limite.pdf", conteudo).Extensao);
    }

    [Fact]
    public void Validar_arquivo_vazio_lanca_excecao()
    {
        Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar("vazio.pdf", []));
    }

    [Theory]
    [InlineData("falso.pdf", "png")]
    [InlineData("falso.png", "pdf")]
    [InlineData("falso.jpg", "pdf")]
    [InlineData("falso.docx", "pdf")]
    public void Validar_conteudo_que_nao_bate_com_a_extensao_lanca_excecao(string nome, string conteudoReal)
    {
        var conteudo = conteudoReal == "png" ? ArquivosDeTeste.Png() : ArquivosDeTeste.Pdf();

        Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar(nome, conteudo));
    }

    [Fact]
    public void Validar_docx_que_e_na_verdade_uma_planilha_lanca_excecao()
    {
        Assert.Throws<ArquivoDocumentoInvalidoException>(
            () => ArquivoDocumento.Validar("troca.docx", ArquivosDeTeste.Zip("xl/workbook.xml")));
    }

    [Fact]
    public void Validar_zip_qualquer_com_extensao_docx_lanca_excecao()
    {
        Assert.Throws<ArquivoDocumentoInvalidoException>(
            () => ArquivoDocumento.Validar("zip.docx", ArquivosDeTeste.Zip("qualquer.txt")));
    }

    [Fact]
    public void Validar_zip_corrompido_com_assinatura_valida_lanca_excecao_e_nao_vaza_a_do_framework()
    {
        byte[] corrompido = [0x50, 0x4B, 0x03, 0x04, 0x01, 0x02, 0x03];

        Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar("quebrado.docx", corrompido));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>.pdf")]
    [InlineData("a\"b.pdf")]
    [InlineData("pasta/ata.pdf")]
    [InlineData("pasta\\ata.pdf")]
    [InlineData("..\\..\\ata.pdf")]
    [InlineData("ata:stream.pdf")]
    [InlineData("ata?.pdf")]
    [InlineData("ata\n.pdf")]
    [InlineData(".pdf")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_nome_invalido_lanca_excecao(string? nome)
    {
        Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar(nome, ArquivosDeTeste.Pdf()));
    }

    [Fact]
    public void Validar_nome_com_mais_de_255_caracteres_lanca_excecao()
    {
        var nome = new string('a', ArquivoDocumento.NomeMaximo - 3) + ".pdf";

        Assert.Throws<ArquivoDocumentoInvalidoException>(() => ArquivoDocumento.Validar(nome, ArquivosDeTeste.Pdf()));
    }

    [Theory]
    [InlineData("Ata nº 3 (assembleia) - 2026.pdf")]
    [InlineData("Regimento_interno v2.1.pdf")]
    [InlineData("Convocação & pauta, março.pdf")]
    public void Validar_nome_com_acentos_e_pontuacao_comum_e_aceito(string nome)
    {
        Assert.Equal(nome, ArquivoDocumento.Validar(nome, ArquivosDeTeste.Pdf()).NomeArquivo);
    }

    [Theory]
    [InlineData("a.pdf", "application/pdf")]
    [InlineData("A.PNG", "image/png")]
    [InlineData("a.desconhecido", "application/octet-stream")]
    public void ContentTypeDe_usa_a_extensao_do_nome_gravado(string nome, string esperado)
    {
        Assert.Equal(esperado, ArquivoDocumento.ContentTypeDe(nome));
    }
}
