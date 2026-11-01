using System.IO.Compression;

namespace euSindico.Application.Tests.Documentos;

/// <summary>Conteúdos mínimos, mas com a assinatura real de cada formato aceito (RN12).</summary>
internal static class ArquivosDeTeste
{
    public static byte[] Pdf() => "%PDF-1.4\n%conteudo de teste\n%%EOF"u8.ToArray();

    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    public static byte[] Jpeg() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    public static byte[] Docx() => Zip("word/document.xml");

    /// <summary>ZIP válido com uma única entrada, de nome à escolha.</summary>
    public static byte[] Zip(string entrada)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var item = zip.CreateEntry(entrada);
            using var escritor = new StreamWriter(item.Open());
            escritor.Write("<x/>");
        }

        return memoria.ToArray();
    }
}
