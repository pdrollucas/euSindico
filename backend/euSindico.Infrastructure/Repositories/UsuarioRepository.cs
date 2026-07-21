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

    public async Task ExcluirUsuarioEDadosRelacionadosAsync(int usuarioId, CancellationToken ct = default)
    {
        // Todas as FKs para usuarios/predios são Restrict (ver *Configuration.cs) — não há
        // ON DELETE CASCADE no banco, então a ordem (filhos antes de prédios antes do usuário)
        // é obrigatória aqui. ExecuteDeleteAsync gera um DELETE direto por tabela, sem carregar
        // as entidades em memória.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var prediosIds = context.Predios
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => p.Id);

        await context.Compromissos.Where(c => prediosIds.Contains(c.PredioId)).ExecuteDeleteAsync(ct);
        await context.Planejamentos.Where(p => prediosIds.Contains(p.PredioId)).ExecuteDeleteAsync(ct);
        await context.Documentos.Where(d => prediosIds.Contains(d.PredioId)).ExecuteDeleteAsync(ct);
        await context.Relatorios.Where(r => prediosIds.Contains(r.PredioId)).ExecuteDeleteAsync(ct);
        await context.Predios.Where(p => p.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await context.RefreshTokens.Where(rt => rt.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await context.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }
}
