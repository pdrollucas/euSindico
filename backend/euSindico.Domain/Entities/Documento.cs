namespace euSindico.Domain.Entities;

public class Documento
{
    public int Id { get; private set; }
    public string NomeArquivo { get; private set; } = string.Empty;
    public string UrlArquivo { get; private set; } = string.Empty;
    public int TipoDocumentoId { get; private set; }
    public int PredioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public TipoDocumento? TipoDocumento { get; private set; }
    public Predio? Predio { get; private set; }

    protected Documento() { }

    public Documento(string nomeArquivo, string urlArquivo, int tipoDocumentoId, int predioId)
    {
        NomeArquivo = nomeArquivo;
        UrlArquivo = urlArquivo;
        TipoDocumentoId = tipoDocumentoId;
        PredioId = predioId;
        CriadoEm = DateTime.UtcNow;
    }
}
