using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IConviteFuncionarioRepository
{
    /// <summary>
    /// Existe algum convite ainda válido (RN17: não expirado, não usado) para este e-mail
    /// neste prédio? Usado para bloquear convites duplicados (RF29).
    /// </summary>
    Task<bool> ExisteConvitePendenteAsync(int predioId, string email, CancellationToken ct = default);

    Task<ConviteFuncionario?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default);
    Task AdicionarAsync(ConviteFuncionario convite, CancellationToken ct = default);

    /// <summary>
    /// O convite já veio de <see cref="BuscarPorHashAsync"/> no mesmo <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
    /// (escopo por requisição), então já está *tracked* — só falta persistir, mesmo padrão de
    /// <see cref="ICodigoRedefinicaoSenhaRepository.AtualizarAsync"/>.
    /// </summary>
    Task AtualizarAsync(ConviteFuncionario convite, CancellationToken ct = default);
}
