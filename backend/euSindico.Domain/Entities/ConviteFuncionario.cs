using euSindico.Domain.Enums;

namespace euSindico.Domain.Entities;

/// <summary>
/// Convite pendente para um funcionário (papel <see cref="PapelPredio.Gestor"/> ou
/// <see cref="PapelPredio.Colaborador"/>) se juntar a um prédio (RF29, RF30). Mesmo princípio de
/// <see cref="CodigoRedefinicaoSenha"/>: o token em texto puro nunca é persistido, só o
/// hash; <see cref="UsadoEm"/> nulo indica convite ainda pendente (RN17).
/// </summary>
public class ConviteFuncionario
{
    public int Id { get; private set; }
    public int PredioId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public PapelPredio Papel { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public int CriadoPorUsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? UsadoEm { get; private set; }

    public Predio? Predio { get; private set; }

    protected ConviteFuncionario() { }

    public ConviteFuncionario(int predioId, string email, PapelPredio papel, string tokenHash, int criadoPorUsuarioId, DateTime expiraEm)
    {
        if (papel == PapelPredio.Sindico)
        {
            throw new ArgumentException("Um convite não pode atribuir o papel Síndico — dono é sempre quem cria o prédio.", nameof(papel));
        }

        PredioId = predioId;
        Email = email;
        Papel = papel;
        TokenHash = tokenHash;
        CriadoPorUsuarioId = criadoPorUsuarioId;
        CriadoEm = DateTime.UtcNow;
        ExpiraEm = expiraEm;
    }

    public bool EstaValido => UsadoEm is null && ExpiraEm > DateTime.UtcNow;

    public void MarcarComoUsado()
    {
        UsadoEm = DateTime.UtcNow;
    }
}
