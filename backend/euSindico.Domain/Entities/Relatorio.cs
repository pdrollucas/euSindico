namespace euSindico.Domain.Entities;

public class Relatorio
{
    public int Id { get; private set; }
    public int MesReferencia { get; private set; }
    public int AnoReferencia { get; private set; }
    public string NomeArquivo { get; private set; } = string.Empty;
    public string UrlArquivo { get; private set; } = string.Empty;
    public int PredioId { get; private set; }
    public DateTime GeradoEm { get; private set; }

    public Predio? Predio { get; private set; }

    protected Relatorio() { }

    public Relatorio(int mesReferencia, int anoReferencia, string nomeArquivo, string urlArquivo, int predioId)
    {
        MesReferencia = mesReferencia;
        AnoReferencia = anoReferencia;
        NomeArquivo = nomeArquivo;
        UrlArquivo = urlArquivo;
        PredioId = predioId;
        GeradoEm = DateTime.UtcNow;
    }
}
