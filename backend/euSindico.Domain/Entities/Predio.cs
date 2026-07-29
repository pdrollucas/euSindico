namespace euSindico.Domain.Entities;

public class Predio
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public int UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public bool Excluido { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }

    public Usuario? Usuario { get; private set; }
    public ICollection<Compromisso> Compromissos { get; private set; } = new List<Compromisso>();
    public ICollection<Planejamento> Planejamentos { get; private set; } = new List<Planejamento>();
    public ICollection<Documento> Documentos { get; private set; } = new List<Documento>();
    public ICollection<Relatorio> Relatorios { get; private set; } = new List<Relatorio>();

    protected Predio() { }

    public Predio(string nome, string endereco, int usuarioId)
    {
        Nome = nome;
        Endereco = endereco;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
        Excluido = false;
    }

    public void AtualizarDados(string nome, string endereco)
    {
        Nome = nome;
        Endereco = endereco;
    }

    public void ExcluirLogicamente()
    {
        Excluido = true;
        ExcluidoEm = DateTime.UtcNow;
    }
}
