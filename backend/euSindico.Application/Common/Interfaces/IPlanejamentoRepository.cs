using euSindico.Application.Planejamentos;
using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IPlanejamentoRepository
{
    /// <summary>Planejamento com <see cref="Planejamento.Predio"/> carregado (o DTO de saída precisa do nome).</summary>
    Task<Planejamento?> BuscarPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Planejamentos de um prédio, já filtrados e ordenados: data decrescente (mais recente primeiro),
    /// com os sem data no fim, por <c>criado_em</c> decrescente. O acesso ao prédio é checado
    /// antes, pelo Service.
    /// </summary>
    Task<(IReadOnlyList<Planejamento> Itens, int Total)> ListarAsync(
        PlanejamentoFiltro filtro, int page, int pageSize, CancellationToken ct = default);

    Task AdicionarAsync(Planejamento planejamento, CancellationToken ct = default);
    Task AtualizarAsync(Planejamento planejamento, CancellationToken ct = default);
    Task RemoverAsync(Planejamento planejamento, CancellationToken ct = default);
}
