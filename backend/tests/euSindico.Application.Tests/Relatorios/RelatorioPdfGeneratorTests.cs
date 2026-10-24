using System.Text;
using euSindico.Application.Relatorios;

namespace euSindico.Application.Tests.Relatorios;

public class RelatorioPdfGeneratorTests
{
    private readonly RelatorioPdfGenerator _sut = new();

    [Fact]
    public void Gerar_produz_um_pdf_valido()
    {
        var pdf = _sut.Gerar(Dados(Item("Visita técnica")));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(pdf.Length > 500);
    }

    [Fact]
    public void Gerar_com_campos_opcionais_ausentes_nao_falha()
    {
        var item = new ItemRelatorio(new DateOnly(2026, 9, 3), new TimeOnly(8, 0), "Inspeção", null, null, "Maria");

        var pdf = _sut.Gerar(Dados(item));

        Assert.NotEmpty(pdf);
    }

    [Fact]
    public void Gerar_com_orcamento_gera_pdf_maior_que_sem_orcamento()
    {
        var sem = _sut.Gerar(Dados(Item("A")));
        var com = _sut.Gerar(Dados(Item("A") with { Orcamento = 1500.50m }));

        Assert.True(com.Length > sem.Length);
    }

    [Fact]
    public void Gerar_com_mistura_de_orcamentos_nao_falha()
    {
        var pdf = _sut.Gerar(Dados(Item("A") with { Orcamento = 0m }, Item("B"), Item("C") with { Orcamento = 99_999_999.99m }));

        Assert.NotEmpty(pdf);
    }

    [Fact]
    public void Gerar_com_muitos_compromissos_quebra_em_varias_paginas()
    {
        var umaPagina = _sut.Gerar(Dados(Item("A")));
        var muitas = _sut.Gerar(Dados(Enumerable.Range(1, 80).Select(i => Item($"Compromisso {i}")).ToArray()));

        Assert.True(muitas.Length > umaPagina.Length);
    }

    private static DadosRelatorio Dados(params ItemRelatorio[] itens) =>
        new("Edifício Aurora", "Rua X, 100", 9, 2026, new DateTime(2026, 10, 1, 10, 0, 0), itens);

    private static ItemRelatorio Item(string titulo) =>
        new(new DateOnly(2026, 9, 3), new TimeOnly(14, 30), titulo, "Atlas Ltda.", "Levar o laudo da última revisão.", "Maria Souza");
}
