using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class ConviteFuncionarioRepository(AppDbContext context) : IConviteFuncionarioRepository
{
    public Task<bool> ExisteConvitePendenteAsync(int predioId, string email, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        return context.ConvitesFuncionario.AnyAsync(
            c => c.PredioId == predioId && c.Email == email && c.UsadoEm == null && c.ExpiraEm > agora, ct);
    }

    public Task<ConviteFuncionario?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.ConvitesFuncionario
            .Include(c => c.Predio)
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, ct);

    public async Task AdicionarAsync(ConviteFuncionario convite, CancellationToken ct = default)
    {
        context.ConvitesFuncionario.Add(convite);
        await context.SaveChangesAsync(ct);
    }

    // O parâmetro já veio de BuscarPorHashAsync no mesmo DbContext (escopo por requisição),
    // então já está tracked — só falta persistir, mesmo padrão de CodigoRedefinicaoSenhaRepository.
    public async Task AtualizarAsync(ConviteFuncionario convite, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}
