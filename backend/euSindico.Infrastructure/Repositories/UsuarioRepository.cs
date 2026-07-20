using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default) =>
        context.Usuarios.AnyAsync(u => u.Email == email, ct);

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken ct = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct = default)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(ct);
    }

    // O parâmetro não precisa ser usado diretamente: o usuário já veio de BuscarPorIdAsync
    // no mesmo DbContext (escopo por requisição), então já está tracked — só falta persistir.
    public async Task AtualizarAsync(Usuario usuario, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}
