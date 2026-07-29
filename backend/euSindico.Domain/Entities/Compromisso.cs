namespace euSindico.Domain.Entities;

public class Compromisso
{
    public int Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public DateOnly DataCompromisso { get; private set; }
    public TimeOnly HorarioCompromisso { get; private set; }
    public string? NomePrestadorServico { get; private set; }
    public string? Detalhes { get; private set; }
    public bool Concluido { get; private set; }
    public int PredioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Predio? Predio { get; private set; }

    protected Compromisso() { }

    public Compromisso(
        string titulo,
        DateOnly dataCompromisso,
        TimeOnly horarioCompromisso,
        int predioId,
        string? nomePrestadorServico = null,
        string? detalhes = null)
    {
        Titulo = titulo;
        DataCompromisso = dataCompromisso;
        HorarioCompromisso = horarioCompromisso;
        PredioId = predioId;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
        Concluido = false;
        CriadoEm = DateTime.UtcNow;
    }

    public void AtualizarDados(
        string titulo,
        DateOnly dataCompromisso,
        TimeOnly horarioCompromisso,
        string? nomePrestadorServico,
        string? detalhes)
    {
        Titulo = titulo;
        DataCompromisso = dataCompromisso;
        HorarioCompromisso = horarioCompromisso;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
    }

    public void Concluir()
    {
        Concluido = true;
    }
}
