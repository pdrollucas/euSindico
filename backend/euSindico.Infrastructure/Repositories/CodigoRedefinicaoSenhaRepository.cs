using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class CodigoRedefinicaoSenhaRepository(AppDbContext context) : ICodigoRedefinicaoSenhaRepository
{
    public Task<CodigoRedefinicaoSenha?> BuscarUltimoDoUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        context.CodigosRedefinicaoSenha
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .FirstOrDefaultAsync(ct);

    public Task<CodigoRedefinicaoSenha?> BuscarPorUsuarioIdEHashAsync(int usuarioId, string codigoHash, CancellationToken ct = default) =>
        context.CodigosRedefinicaoSenha.FirstOrDefaultAsync(
            c => c.UsuarioId == usuarioId && c.CodigoHash == codigoHash, ct);

    public async Task AdicionarAsync(CodigoRedefinicaoSenha codigo, CancellationToken ct = default)
    {
        context.CodigosRedefinicaoSenha.Add(codigo);
        await context.SaveChangesAsync(ct);
    }

    // O parâmetro já veio de BuscarPorUsuarioIdEHashAsync no mesmo DbContext (escopo por
    // requisição), então já está tracked — só falta persistir, igual ao RefreshTokenRepository.
    public async Task AtualizarAsync(CodigoRedefinicaoSenha codigo, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task InvalidarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        await context.CodigosRedefinicaoSenha
            .Where(c => c.UsuarioId == usuarioId && c.UsadoEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.UsadoEm, agora), ct);
    }
}