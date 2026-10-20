using euSindico.Application.Common.Interfaces;
using euSindico.Application.Compromissos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class CompromissoRepository(AppDbContext context) : ICompromissoRepository
{
    public Task<Compromisso?> BuscarPorIdAsync(int id, CancellationToken ct = default) =>
        context.Compromissos
            .Include(c => c.Predio)
            .Include(c => c.Responsavel)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(IReadOnlyList<Compromisso> Itens, int Total)> ListarAsync(
        CompromissoFiltro filtro, int page, int pageSize, CancellationToken ct = default)
    {
        var usuarioId = filtro.UsuarioId;
        var verTodos = filtro.Escopo == EscopoCompromissos.Todos;
        var hoje = filtro.Hoje;
        var agora = filtro.Agora;

        // Visível = o usuário tem vínculo com o prédio (RN16) e o compromisso é dele — ou, no
        // escopo "todos", o papel dele naquele prédio não é Colaborador (RN21). O papel é por
        // prédio (RN18), então a regra é avaliada por linha, não uma vez só para o usuário.
        var query = context.Compromissos
            .AsNoTracking()
            .Where(c => !c.Predio!.Excluido)
            .Where(c => context.PredioUsuarios.Any(pu =>
                pu.PredioId == c.PredioId
                && pu.UsuarioId == usuarioId
                && (c.ResponsavelUsuarioId == usuarioId || (verTodos && pu.Papel != PapelPredio.Colaborador))));

        if (filtro.PredioId is { } predioId)
        {
            query = query.Where(c => c.PredioId == predioId);
        }

        if (filtro.Titulo is { } titulo)
        {
            query = query.Where(c => c.Titulo.Contains(titulo));
        }

        query = filtro.Situacao switch
        {
            SituacaoCompromissos.Proximos => query.Where(c => !c.Concluido && c.DataCompromisso >= hoje),
            SituacaoCompromissos.Hoje => query.Where(c => !c.Concluido && c.DataCompromisso == hoje),
            SituacaoCompromissos.Atrasados => query.Where(c =>
                !c.Concluido
                && (c.DataCompromisso < hoje || (c.DataCompromisso == hoje && c.HorarioCompromisso < agora))),
            SituacaoCompromissos.Concluidos => query.Where(c => c.Concluido),
            _ => query,
        };

        var total = await query.CountAsync(ct);

        // RN14: ordem cronológica crescente. Id desempata para a paginação ser estável.
        var itens = await query
            .Include(c => c.Predio)
            .Include(c => c.Responsavel)
            .OrderBy(c => c.DataCompromisso)
            .ThenBy(c => c.HorarioCompromisso)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task AdicionarAsync(Compromisso compromisso, CancellationToken ct = default)
    {
        context.Compromissos.Add(compromisso);
        await context.SaveChangesAsync(ct);
    }

    // O compromisso já veio de BuscarPorIdAsync no mesmo DbContext (escopo por requisição),
    // então já está tracked — só falta persistir.
    public async Task AtualizarAsync(Compromisso compromisso, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task RemoverAsync(Compromisso compromisso, CancellationToken ct = default)
    {
        context.Compromissos.Remove(compromisso);
        await context.SaveChangesAsync(ct);
    }
}
