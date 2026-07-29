namespace euSindico.Domain.Entities;

public class RefreshToken
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? RevogadoEm { get; private set; }

    public Usuario? Usuario { get; private set; }

    protected RefreshToken() { }

    public RefreshToken(int usuarioId, string tokenHash, DateTime expiraEm)
    {
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        CriadoEm = DateTime.UtcNow;
        ExpiraEm = expiraEm;
    }

    public bool EstaAtivo => RevogadoEm is null && ExpiraEm > DateTime.UtcNow;

    public void Revogar()
    {
        RevogadoEm = DateTime.UtcNow;
    }
}
