using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace euSindico.Application.Relatorios;

/// <summary>
/// Monta o nome do PDF baixado: <c>{NomeDoPredio}_Relatorio_{AAAA-MM}.pdf</c>. O nome do prédio é
/// texto do usuário, então vira um "slug" seguro para qualquer sistema de arquivos e navegador —
/// sem acento, espaço, barra ou símbolo (isso também impede path traversal e quebra de cabeçalho
/// no download).
/// </summary>
public static partial class NomeArquivoRelatorio
{
    private const int TamanhoMaximoPredio = 80;
    private const string PredioPadrao = "Predio";

    public static string Montar(string predioNome, int mes, int ano) =>
        $"{Slug(predioNome)}_Relatorio_{ano}-{mes:D2}.pdf";

    internal static string Slug(string texto)
    {
        var semAcento = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                semAcento.Append(c);
            }
        }

        var slug = NaoAlfanumerico().Replace(semAcento.ToString(), "-").Trim('-');
        if (slug.Length > TamanhoMaximoPredio)
        {
            slug = slug[..TamanhoMaximoPredio].Trim('-');
        }

        return slug.Length == 0 ? PredioPadrao : slug;
    }

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NaoAlfanumerico();
}
