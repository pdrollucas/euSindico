using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IUsuarioRepository
{
    Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct = default);
}
