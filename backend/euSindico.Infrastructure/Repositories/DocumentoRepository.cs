using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class DocumentoRepository(AppDbContext context) : IDocumentoRepository
{
    public Task<Documento?> BuscarPorIdAsync(int id, CancellationToken ct = default) =>
        context.Documentos
            .Include(d => d.Predio)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
        int predioId, TipoDocumentoPredio tipo, string? nome, int page, int pageSize, CancellationToken ct = default)
    {
        var tipoId = (int)tipo;

        var query = context.Documentos
            .AsNoTracking()
            .Where(d => d.PredioId == predioId && d.TipoDocumentoId == tipoId && !d.Predio!.Excluido);

        if (nome is not null)
        {
            query = query.Where(d => d.NomeArquivo.Contains(nome));
        }

        var total = await query.CountAsync(ct);

        // Enviado mais recentemente primeiro; id desempata para a paginação ser estável.
        var itens = await query
            .Include(d => d.Predio)
            .OrderByDescending(d => d.CriadoEm)
            .ThenByDescending(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itens, total);
    }

    public Task<int> ContarAsync(
        int predioId, TipoDocumentoPredio tipo, DateTime? deUtc = null, DateTime? ateUtc = null, CancellationToken ct = default)
    {
        var tipoId = (int)tipo;
        var query = context.Documentos.Where(d => d.PredioId == predioId && d.TipoDocumentoId == tipoId);

        if (deUtc is { } de)
        {
            query = query.Where(d => d.CriadoEm >= de);
        }

        if (ateUtc is { } ate)
        {
            query = query.Where(d => d.CriadoEm < ate);
        }

        return query.CountAsync(ct);
    }

    public async Task AdicionarAsync(Documento documento, CancellationToken ct = default)
    {
        context.Documentos.Add(documento);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Documento documento, CancellationToken ct = default)
    {
        context.Documentos.Remove(documento);
        await context.SaveChangesAsync(ct);
    }
}
