namespace euSindico.Domain.Entities;

public class Planejamento
{
    public int Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public DateOnly? DataPlanejamento { get; private set; }
    public decimal? OrcamentoPrevisto { get; private set; }
    public string? NomePrestadorServico { get; private set; }
    public string? Detalhes { get; private set; }
    public int PredioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Predio? Predio { get; private set; }

    protected Planejamento() { }

    public Planejamento(
        string titulo,
        int predioId,
        DateOnly? dataPlanejamento = null,
        decimal? orcamentoPrevisto = null,
        string? nomePrestadorServico = null,
        string? detalhes = null)
    {
        Titulo = titulo;
        PredioId = predioId;
        DataPlanejamento = dataPlanejamento;
        OrcamentoPrevisto = orcamentoPrevisto;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
        CriadoEm = DateTime.UtcNow;
    }

    public void AtualizarDados(
        string titulo,
        DateOnly? dataPlanejamento,
        decimal? orcamentoPrevisto,
        string? nomePrestadorServico,
        string? detalhes)
    {
        Titulo = titulo;
        DataPlanejamento = dataPlanejamento;
        OrcamentoPrevisto = orcamentoPrevisto;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
    }
}
