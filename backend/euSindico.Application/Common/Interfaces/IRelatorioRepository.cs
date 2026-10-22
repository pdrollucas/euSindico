using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface IRelatorioRepository
{
    /// <summary>Relatório com <see cref="Relatorio.Predio"/> carregado.</summary>
    Task<Relatorio?> BuscarPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Relatórios de um prédio, do gerado mais recentemente para o mais antigo (cada geração é
    /// preservada, RN33). O acesso ao prédio é checado antes, pelo Service.
    /// </summary>
    Task<(IReadOnlyList<Relatorio> Itens, int Total)> ListarAsync(
        int predioId, int? ano, int? mes, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Quantos relatórios existem para o prédio e o mês/ano de referência (limite de gerações, RN36).</summary>
    Task<int> ContarAsync(int predioId, int ano, int mes, CancellationToken ct = default);

    Task AdicionarAsync(Relatorio relatorio, CancellationToken ct = default);
    Task RemoverAsync(Relatorio relatorio, CancellationToken ct = default);
}
