namespace euSindico.Domain.Entities;

public class TipoDocumento
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;

    public ICollection<Documento> Documentos { get; private set; } = new List<Documento>();

    protected TipoDocumento() { }

    public TipoDocumento(string nome)
    {
        Nome = nome;
    }
}
