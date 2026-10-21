using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class RelatorioRepository(AppDbContext context) : IRelatorioRepository
{
    public Task<Relatorio?> BuscarPorIdAsync(int id, CancellationToken ct = default) =>
        context.Relatorios
            .Include(r => r.Predio)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IReadOnlyList<Relatorio> Itens, int Total)> ListarAsync(
        int predioId, int? ano, int? mes, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Relatorios
            .AsNoTracking()
            .Where(r => r.PredioId == predioId && !r.Predio!.Excluido);

        if (ano is { } anoFiltro)
        {
            query = query.Where(r => r.AnoReferencia == anoFiltro);
        }

        if (mes is { } mesFiltro)
        {
            query = query.Where(r => r.MesReferencia == mesFiltro);
        }

        var total = await query.CountAsync(ct);

        // Gerado mais recentemente primeiro; id desempata para a paginação ser estável.
        var itens = await query
            .Include(r => r.Predio)
            .OrderByDescending(r => r.GeradoEm)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itens, total);
    }

    public Task<int> ContarAsync(int predioId, int ano, int mes, CancellationToken ct = default) =>
        context.Relatorios.CountAsync(
            r => r.PredioId == predioId && r.AnoReferencia == ano && r.MesReferencia == mes, ct);

    public async Task RemoverAsync(Relatorio relatorio, CancellationToken ct = default)
    {
        context.Relatorios.Remove(relatorio);
        await context.SaveChangesAsync(ct);
    }

    public async Task AdicionarAsync(Relatorio relatorio, CancellationToken ct = default)
    {
        context.Relatorios.Add(relatorio);
        await context.SaveChangesAsync(ct);
    }
}
