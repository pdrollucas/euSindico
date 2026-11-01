using euSindico.Application.Documentos;

namespace euSindico.Application.Tests.Documentos;

public class ChaveDocumentoTests
{
    [Fact]
    public void Prefixo_termina_com_barra_para_nao_casar_com_outro_predio()
    {
        Assert.Equal("documentos/1/", ChaveDocumento.Prefixo(1));
        Assert.False(ChaveDocumento.Prefixo(10).StartsWith(ChaveDocumento.Prefixo(1), StringComparison.Ordinal));
    }

    [Fact]
    public void Nova_fica_sob_o_prefixo_do_predio_e_preserva_a_extensao()
    {
        var chave = ChaveDocumento.Nova(7, ".pdf");

        Assert.StartsWith(ChaveDocumento.Prefixo(7), chave);
        Assert.EndsWith(".pdf", chave);
    }

    [Fact]
    public void Nova_gera_chaves_diferentes_a_cada_chamada()
    {
        Assert.NotEqual(ChaveDocumento.Nova(7, ".pdf"), ChaveDocumento.Nova(7, ".pdf"));
    }
}
