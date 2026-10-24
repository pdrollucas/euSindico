namespace euSindico.Domain.Entities;

public class Compromisso
{
    public int Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public DateOnly DataCompromisso { get; private set; }
    public TimeOnly HorarioCompromisso { get; private set; }
    public string? NomePrestadorServico { get; private set; }
    public decimal? Orcamento { get; private set; }
    public string? Detalhes { get; private set; }
    public bool Concluido { get; private set; }
    public int PredioId { get; private set; }
    public int ResponsavelUsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Predio? Predio { get; private set; }
    public Usuario? Responsavel { get; private set; }

    protected Compromisso() { }

    public Compromisso(
        string titulo,
        DateOnly dataCompromisso,
        TimeOnly horarioCompromisso,
        int predioId,
        int responsavelUsuarioId,
        string? nomePrestadorServico = null,
        string? detalhes = null,
        decimal? orcamento = null)
    {
        Titulo = titulo;
        DataCompromisso = dataCompromisso;
        HorarioCompromisso = horarioCompromisso;
        PredioId = predioId;
        ResponsavelUsuarioId = responsavelUsuarioId;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
        Orcamento = orcamento;
        Concluido = false;
        CriadoEm = DateTime.UtcNow;
    }

    public void AtualizarDados(
        string titulo,
        DateOnly dataCompromisso,
        TimeOnly horarioCompromisso,
        string? nomePrestadorServico,
        string? detalhes,
        int predioId,
        int responsavelUsuarioId,
        decimal? orcamento)
    {
        Titulo = titulo;
        DataCompromisso = dataCompromisso;
        HorarioCompromisso = horarioCompromisso;
        NomePrestadorServico = nomePrestadorServico;
        Detalhes = detalhes;
        Orcamento = orcamento;
        PredioId = predioId;
        ResponsavelUsuarioId = responsavelUsuarioId;
    }

    // Reversível de propósito (RF17): o usuário marca pelo celular e pode clicar errado.
    public void Concluir()
    {
        Concluido = true;
    }

    public void Reabrir()
    {
        Concluido = false;
    }

    // Usado quando o responsável exclui a própria conta (RF07) — o compromisso fica com o dono do prédio.
    public void AtribuirResponsavel(int responsavelUsuarioId)
    {
        ResponsavelUsuarioId = responsavelUsuarioId;
    }
}
