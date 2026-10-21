using euSindico.Application.Relatorios;

namespace euSindico.Application.Tests.Relatorios;

public class NomeArquivoRelatorioTests
{
    [Theory]
    [InlineData("Edifício Aurora", 9, 2026, "Edificio-Aurora_Relatorio_2026-09.pdf")]
    [InlineData("Residencial São João & Filhos", 12, 2025, "Residencial-Sao-Joao-Filhos_Relatorio_2025-12.pdf")]
    [InlineData("  Torre   Norte  ", 1, 2026, "Torre-Norte_Relatorio_2026-01.pdf")]
    [InlineData("Bloco A/B (Fase 2)", 3, 2026, "Bloco-A-B-Fase-2_Relatorio_2026-03.pdf")]
    public void Montar_gera_nome_sem_acento_espaco_ou_simbolo(string predio, int mes, int ano, string esperado)
    {
        Assert.Equal(esperado, NomeArquivoRelatorio.Montar(predio, mes, ano));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("a\\b\"c\r\nd")]
    public void Montar_nao_deixa_passar_separadores_nem_quebras_de_linha(string predio)
    {
        var nome = NomeArquivoRelatorio.Montar(predio, 9, 2026);

        Assert.DoesNotContain('/', nome);
        Assert.DoesNotContain('\\', nome);
        Assert.DoesNotContain('"', nome);
        Assert.DoesNotContain('\r', nome);
        Assert.DoesNotContain('\n', nome);
        Assert.DoesNotContain("..", nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("###")]
    [InlineData("日本語")]
    public void Montar_sem_caracteres_aproveitaveis_usa_nome_padrao(string predio)
    {
        Assert.Equal("Predio_Relatorio_2026-09.pdf", NomeArquivoRelatorio.Montar(predio, 9, 2026));
    }

    [Fact]
    public void Montar_limita_o_tamanho_do_nome_do_predio()
    {
        var nome = NomeArquivoRelatorio.Montar(new string('a', 300), 9, 2026);

        Assert.True(nome.Length <= 80 + "_Relatorio_2026-09.pdf".Length);
        Assert.EndsWith("_Relatorio_2026-09.pdf", nome);
    }
}
