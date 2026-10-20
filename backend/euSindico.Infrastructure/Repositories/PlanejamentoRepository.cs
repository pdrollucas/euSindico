using euSindico.Application.Common.Interfaces;
using euSindico.Application.Planejamentos;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class PlanejamentoRepository(AppDbContext context) : IPlanejamentoRepository
{
    public Task<Planejamento?> BuscarPorIdAsync(int id, CancellationToken ct = default) =>
        context.Planejamentos
            .Include(p => p.Predio)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Planejamento> Itens, int Total)> ListarAsync(
        PlanejamentoFiltro filtro, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Planejamentos
            .AsNoTracking()
            .Where(p => p.PredioId == filtro.PredioId && !p.Predio!.Excluido);

        if (filtro.Titulo is { } titulo)
        {
            query = query.Where(p => p.Titulo.Contains(titulo));
        }

        // Planejamento sem orçamento (NULL) não casa com a faixa: a comparação com NULL nunca é verdadeira.
        if (filtro.OrcamentoMinimo is { } minimo)
        {
            query = query.Where(p => p.OrcamentoPrevisto >= minimo);
        }

        if (filtro.OrcamentoMaximo is { } maximo)
        {
            query = query.Where(p => p.OrcamentoPrevisto <= maximo);
        }

        var total = await query.CountAsync(ct);

        // Com data primeiro (mais recente no topo), sem data no fim; em ambos, o cadastrado mais
        // recentemente vem antes. Id desempata para a paginação ser estável.
        var itens = await query
            .Include(p => p.Predio)
            .OrderBy(p => p.DataPlanejamento == null)
            .ThenByDescending(p => p.DataPlanejamento)
            .ThenByDescending(p => p.CriadoEm)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task AdicionarAsync(Planejamento planejamento, CancellationToken ct = default)
    {
        context.Planejamentos.Add(planejamento);
        await context.SaveChangesAsync(ct);
    }

    // O planejamento já veio de BuscarPorIdAsync no mesmo DbContext (escopo por requisição),
    // então já está tracked — só falta persistir.
    public async Task AtualizarAsync(Planejamento planejamento, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task RemoverAsync(Planejamento planejamento, CancellationToken ct = default)
    {
        context.Planejamentos.Remove(planejamento);
        await context.SaveChangesAsync(ct);
    }
}
