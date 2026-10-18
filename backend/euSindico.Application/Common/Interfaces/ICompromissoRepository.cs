using euSindico.Application.Compromissos;
using euSindico.Domain.Entities;

namespace euSindico.Application.Common.Interfaces;

public interface ICompromissoRepository
{
    /// <summary>
    /// Compromisso com <see cref="Compromisso.Predio"/> e <see cref="Compromisso.Responsavel"/>
    /// carregados (o DTO de saída precisa dos dois nomes).
    /// </summary>
    Task<Compromisso?> BuscarPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Compromissos visíveis ao usuário (RN16, RN21, RF34), já filtrados por situação e ordenados
    /// por data e horário crescentes (RN14). Ignora prédios excluídos (RN08). Em prédios onde o
    /// usuário é Colaborador só devolve os dele, independente do escopo.
    /// </summary>
    Task<(IReadOnlyList<Compromisso> Itens, int Total)> ListarAsync(
        CompromissoFiltro filtro, int page, int pageSize, CancellationToken ct = default);

    Task AdicionarAsync(Compromisso compromisso, CancellationToken ct = default);
    Task AtualizarAsync(Compromisso compromisso, CancellationToken ct = default);
    Task RemoverAsync(Compromisso compromisso, CancellationToken ct = default);
}
