namespace euSindico.Domain.Entities;

public class CodigoTrocaEmail
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string NovoEmail { get; private set; } = string.Empty;
    public string CodigoHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? UsadoEm { get; private set; }

    public Usuario? Usuario { get; private set; }

    protected CodigoTrocaEmail() { }

    public CodigoTrocaEmail(int usuarioId, string novoEmail, string codigoHash, DateTime expiraEm)
    {
        UsuarioId = usuarioId;
        NovoEmail = novoEmail;
        CodigoHash = codigoHash;
        CriadoEm = DateTime.UtcNow;
        ExpiraEm = expiraEm;
    }

    public bool EstaValido => UsadoEm is null && ExpiraEm > DateTime.UtcNow;

    public void MarcarComoUsado()
    {
        UsadoEm = DateTime.UtcNow;
    }
}
