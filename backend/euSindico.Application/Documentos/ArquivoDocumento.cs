using System.IO.Compression;
using euSindico.Application.Common.Exceptions;

namespace euSindico.Application.Documentos;

/// <summary>
/// Valida o arquivo de um documento (RN12, RNF09): formato PDF, DOCX, JPG ou PNG, até 20 MB.
/// Não confia no que o cliente declara — nem no <c>Content-Type</c> nem só na extensão: o conteúdo
/// precisa começar com a assinatura (magic bytes) do formato, e DOCX (que é um ZIP) precisa ter
/// a estrutura do Word. O <c>ContentType</c> devolvido sai da extensão validada, nunca do cliente.
/// </summary>
public static class ArquivoDocumento
{
    public const int TamanhoMaximoBytes = 20 * 1024 * 1024;
    public const int NomeMaximo = 255;

    public const string MensagemFormato = "Formato de arquivo não aceito. Envie PDF, DOCX, JPG ou PNG.";
    public static readonly string MensagemTamanho = $"O arquivo deve ter no máximo {TamanhoMaximoBytes / (1024 * 1024)} MB.";

    private const string MensagemNome = "O nome do arquivo é inválido.";
    private const string MensagemConteudo = "O conteúdo do arquivo não corresponde ao formato informado.";
    private const string MensagemVazio = "O arquivo está vazio.";

    // Caracteres que nunca fazem parte de um nome legítimo e abrem caminho para XSS (< >, aspas, crase,
    // chaves) ou path traversal / cabeçalho quebrado (barras, dois-pontos, controle). Mesma linha do
    // restante do projeto: rejeitar na entrada em vez de sanitizar (SECURITY.md, seção 3).
    private static readonly char[] NomeProibido = ['<', '>', '"', '`', '{', '}', '|', '\\', '/', ':', '*', '?'];

    private static readonly Dictionary<string, string> ContentTypePorExtensao = new(StringComparer.Ordinal)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
    };

    private static readonly byte[] AssinaturaPdf = "%PDF-"u8.ToArray();
    private static readonly byte[] AssinaturaZip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] AssinaturaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Resultado da validação: o nome já aparado, a extensão em minúsculas (com ponto) e o content type.</summary>
    public record Validado(string NomeArquivo, string Extensao, string ContentType);

    public static Validado Validar(string? nomeArquivo, byte[] conteudo)
    {
        var nome = ValidarNome(nomeArquivo);

        var extensao = Path.GetExtension(nome).ToLowerInvariant();
        if (!ContentTypePorExtensao.TryGetValue(extensao, out var contentType))
        {
            throw new ArquivoDocumentoInvalidoException(MensagemFormato);
        }

        if (conteudo.Length == 0)
        {
            throw new ArquivoDocumentoInvalidoException(MensagemVazio);
        }

        if (conteudo.Length > TamanhoMaximoBytes)
        {
            throw new ArquivoDocumentoInvalidoException(MensagemTamanho);
        }

        if (!ConteudoCorrespondeAExtensao(extensao, conteudo))
        {
            throw new ArquivoDocumentoInvalidoException(MensagemConteudo);
        }

        return new Validado(nome, extensao, contentType);
    }

    /// <summary>Content type de um arquivo já aceito, pela extensão do nome gravado.</summary>
    public static string ContentTypeDe(string nomeArquivo) =>
        ContentTypePorExtensao.GetValueOrDefault(Path.GetExtension(nomeArquivo).ToLowerInvariant(), "application/octet-stream");

    private static string ValidarNome(string? nomeArquivo)
    {
        var nome = nomeArquivo?.Trim();

        if (string.IsNullOrEmpty(nome)
            || nome.Length > NomeMaximo
            || nome.IndexOfAny(NomeProibido) >= 0
            || nome.Any(char.IsControl)
            || nome.StartsWith('.'))
        {
            throw new ArquivoDocumentoInvalidoException(MensagemNome);
        }

        return nome;
    }

    private static bool ConteudoCorrespondeAExtensao(string extensao, byte[] conteudo) => extensao switch
    {
        ".pdf" => Comeca(conteudo, AssinaturaPdf),
        ".jpg" or ".jpeg" => Comeca(conteudo, AssinaturaJpeg),
        ".png" => Comeca(conteudo, AssinaturaPng),
        ".docx" => Comeca(conteudo, AssinaturaZip) && ZipContem(conteudo, "word/document.xml"),
        _ => false,
    };

    private static bool Comeca(byte[] conteudo, byte[] assinatura) =>
        conteudo.AsSpan().StartsWith(assinatura);

    // Só lê o índice do ZIP (nomes das entradas), nunca descomprime nada — sem risco de "zip bomb".
    private static bool ZipContem(byte[] conteudo, string entrada)
    {
        try
        {
            using var stream = new MemoryStream(conteudo, writable: false);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            return zip.GetEntry(entrada) is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
