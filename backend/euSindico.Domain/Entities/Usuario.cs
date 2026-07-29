namespace euSindico.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    public ICollection<Predio> Predios { get; private set; } = new List<Predio>();

    protected Usuario() { }

    public Usuario(string nome, string email, string senhaHash)
    {
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        CriadoEm = DateTime.UtcNow;
    }

    public void AtualizarPerfil(string nome, string email)
    {
        Nome = nome;
        Email = email;
    }

    public void AlterarSenha(string novaSenhaHash)
    {
        SenhaHash = novaSenhaHash;
    }
}
