using euSindico.Domain.Enums;

namespace euSindico.Domain.Entities;

/// <summary>
/// Vínculo entre um usuário e um prédio, com um papel de acesso (RN16, RN18). Todo prédio
/// nasce com uma linha aqui para o próprio dono (<see cref="CriarComoDono"/>); funcionários
/// ganham a deles ao aceitar um convite (<see cref="CriarComoConvidado"/>).
/// </summary>
public class PredioUsuario
{
    public int Id { get; private set; }
    public int PredioId { get; private set; }
    public int UsuarioId { get; private set; }
    public PapelPredio Papel { get; private set; }
    public int? ConvidadoPorUsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Predio? Predio { get; private set; }
    public Usuario? Usuario { get; private set; }

    protected PredioUsuario() { }

    private PredioUsuario(int predioId, int usuarioId, PapelPredio papel, int? convidadoPorUsuarioId)
    {
        PredioId = predioId;
        UsuarioId = usuarioId;
        Papel = papel;
        ConvidadoPorUsuarioId = convidadoPorUsuarioId;
        CriadoEm = DateTime.UtcNow;
    }

    public static PredioUsuario CriarComoDono(int predioId, int usuarioId) =>
        new(predioId, usuarioId, PapelPredio.Sindico, convidadoPorUsuarioId: null);

    public static PredioUsuario CriarComoConvidado(int predioId, int usuarioId, PapelPredio papel, int convidadoPorUsuarioId)
    {
        if (papel == PapelPredio.Sindico)
        {
            throw new ArgumentException("Um convite não pode atribuir o papel Síndico — dono é sempre quem cria o prédio.", nameof(papel));
        }

        return new PredioUsuario(predioId, usuarioId, papel, convidadoPorUsuarioId);
    }
}
