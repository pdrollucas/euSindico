using System.Globalization;
using euSindico.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace euSindico.Application.Relatorios;

// RN13 — PDF com os compromissos concluídos do mês, em ordem cronológica (a ordem já vem pronta em
// DadosRelatorio). Os textos entram como texto puro no documento, sem interpretação de HTML.
// Visual: segue o tema do front (DESIGN.md) — o azul da marca fica só no título e nos divisores; o resto é preto.
public class RelatorioPdfGenerator : IRelatorioPdfGenerator
{
    // Mesmos valores de frontend/src/plugins/vuetify.ts.
    private const string AzulMarca = "#1E4FA8";

    // O QuestPDF usa as fontes instaladas no sistema. Times New Roman existe no Windows; em Linux
    // (container) cai para a Liberation Serif, que tem as mesmas métricas — ver RELATORIOS.md.
    private const string FontePrincipal = "Times New Roman";
    private const string FonteAlternativa = "Liberation Serif";

    private static readonly CultureInfo Cultura = new("pt-BR");

    static RelatorioPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Gerar(DadosRelatorio dados)
    {
        var periodo = $"{Cultura.TextInfo.ToTitleCase(Cultura.DateTimeFormat.GetMonthName(dados.Mes))} de {dados.Ano}";

        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(40);
                pagina.DefaultTextStyle(t => t
                    .FontFamily(FontePrincipal, FonteAlternativa)
                    .FontSize(12)
                    .FontColor(Colors.Black)
                    .LineHeight(1.3f));

                pagina.Header().AlignRight()
                    .Text("Arquivo gerado pelo sistema euSíndico")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                pagina.Content().PaddingTop(10).Column(coluna =>
                {
                    coluna.Item().Text("Relatório de atividades")
                        .FontSize(20).Bold().FontColor(AzulMarca);

                    coluna.Item().PaddingTop(18).Column(c =>
                    {
                        c.Item().Element(e => Campo(e, "Nome do prédio", dados.PredioNome));
                        c.Item().Element(e => Campo(e, "Endereço", dados.PredioEndereco));
                    });

                    coluna.Item().PaddingTop(14).Column(c =>
                    {
                        c.Item().Element(e => Campo(e, "Compromissos concluídos no período de", periodo));
                        c.Item().Element(e => Campo(e, "Gerado em", $"{dados.GeradoEm:dd/MM/yyyy HH:mm}"));
                    });

                    var comOrcamento = dados.Compromissos.Where(i => i.Orcamento is not null).ToList();
                    var semOrcamento = dados.Compromissos.Count - comOrcamento.Count;

                    coluna.Item().PaddingTop(14).Column(c =>
                    {
                        c.Item().Element(e =>
                            Campo(e, "Total de compromissos concluídos", dados.Compromissos.Count.ToString(Cultura)));

                        // O total soma só quem tem orçamento informado; a contagem dos demais evita que
                        // o valor seja lido como o gasto completo do mês.
                        if (comOrcamento.Count > 0)
                        {
                            c.Item().Element(e =>
                                Campo(e, "Orçamento total do mês", Moeda(comOrcamento.Sum(i => i.Orcamento!.Value))));
                        }

                        if (semOrcamento > 0)
                        {
                            c.Item().Element(e =>
                                Campo(e, "Compromissos sem orçamento informado", semOrcamento.ToString(Cultura)));
                        }
                    });

                    coluna.Item().PaddingTop(16).Element(Divisor);

                    foreach (var item in dados.Compromissos)
                    {
                        // Cada compromisso fica inteiro na mesma página; o divisor de baixo também.
                        coluna.Item().ShowEntire().PaddingVertical(10).Column(c =>
                        {
                            c.Item().Element(e => Campo(e, "Título", item.Titulo));
                            c.Item().Element(e => Campo(e, "Data", $"{item.Data:dd/MM/yyyy} às {item.Horario:HH:mm}"));
                            c.Item().Element(e => Campo(e, "Responsável", item.ResponsavelNome));

                            if (item.NomePrestadorServico is not null)
                            {
                                c.Item().Element(e => Campo(e, "Prestador de serviço", item.NomePrestadorServico));
                            }

                            if (item.Orcamento is not null)
                            {
                                c.Item().Element(e => Campo(e, "Orçamento", Moeda(item.Orcamento.Value)));
                            }

                            if (item.Detalhes is not null)
                            {
                                c.Item().Element(e => Campo(e, "Detalhes", item.Detalhes));
                            }
                        });

                        coluna.Item().Element(Divisor);
                    }
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Grey.Darken1));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // "Rótulo: valor" — rótulo em negrito, valor em texto normal, ambos pretos.
    private static void Campo(IContainer container, string rotulo, string valor) =>
        container.Text(texto =>
        {
            texto.Span($"{rotulo}: ").Bold();
            texto.Span(valor);
        });

    // "R$ 1.500,50" — sempre pt-BR, independente da cultura do servidor.
    private static string Moeda(decimal valor) => valor.ToString("C2", Cultura);

    private static void Divisor(IContainer container) =>
        container.LineHorizontal(1).LineColor(AzulMarca);
}
