using euSindico.Application.Relatorios;

namespace euSindico.Application.Tests.Relatorios;

public class ChaveRelatorioTests
{
    [Fact]
    public void Prefixo_termina_em_barra_para_nao_casar_com_outro_predio()
    {
        Assert.Equal("relatorios/1/", ChaveRelatorio.Prefixo(1));
        Assert.False(ChaveRelatorio.Nova(10).StartsWith(ChaveRelatorio.Prefixo(1), StringComparison.Ordinal));
    }

    [Fact]
    public void Nova_fica_sob_o_prefixo_do_predio_e_e_unica()
    {
        var a = ChaveRelatorio.Nova(7);
        var b = ChaveRelatorio.Nova(7);

        Assert.StartsWith(ChaveRelatorio.Prefixo(7), a, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", a);
        Assert.NotEqual(a, b);
    }
}
