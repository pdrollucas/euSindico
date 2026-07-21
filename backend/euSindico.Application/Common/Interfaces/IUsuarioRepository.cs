using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IUsuarioRepository
{
    Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken ct = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct = default);
    Task AtualizarAsync(Usuario usuario, CancellationToken ct = default);

    /// <summary>
    /// Exclui definitivamente o usuário e todos os dados vinculados (prédios, compromissos,
    /// planejamentos, documentos, relatórios e refresh tokens) — ver AUTHENTICATION.md, Fluxo 7.
    /// </summary>
    Task ExcluirUsuarioEDadosRelacionadosAsync(int usuarioId, CancellationToken ct = default);
}
