namespace euSindico.Application.Common.Dtos;

// Envelope genérico de listagem paginada (RNF10) — primeiro endpoint paginado do backend
// (PREDIOS.md, Fluxo 2), reaproveitável por Compromissos/Planejamentos/Documentos/Relatórios.
public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
