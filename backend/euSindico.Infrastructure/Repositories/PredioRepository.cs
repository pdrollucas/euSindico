using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Repositories;

public class PredioRepository(AppDbContext context) : IPredioRepository
{
    public Task<int> ContarAtivosDoUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        context.Predios.CountAsync(p => p.UsuarioId == usuarioId && !p.Excluido, ct);

    public Task<bool> ExisteNomeEEnderecoAtivoAsync(int usuarioId, string nome, string endereco, CancellationToken ct = default) =>
        context.Predios.AnyAsync(p => p.UsuarioId == usuarioId && p.Nome == nome && p.Endereco == endereco && !p.Excluido, ct);

    public async Task<Predio> AdicionarComDonoAsync(Predio predio, CancellationToken ct = default)
    {
        // Transação explícita (mesmo padrão de UsuarioRepository.ExcluirUsuarioEDadosRelacionadosAsync):
        // o segundo INSERT depende do Id gerado pelo primeiro, então não dá pra rastrear os dois
        // como um único grafo e salvar de uma vez só — mas ambos precisam ser tudo ou nada.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        context.Predios.Add(predio);
        await context.SaveChangesAsync(ct);

        context.PredioUsuarios.Add(PredioUsuario.CriarComoDono(predio.Id, predio.UsuarioId));
        await context.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return predio;
    }
}
