using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Common.Interfaces;

public interface IDocumentoRepository
{
    /// <summary>Documento com <see cref="Documento.Predio"/> carregado.</summary>
    Task<Documento?> BuscarPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Documentos de um prédio e tipo, do enviado mais recentemente para o mais antigo, com busca
    /// opcional por trecho do nome. O acesso ao prédio é checado antes, pelo Service.
    /// </summary>
    Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
        int predioId, TipoDocumentoPredio tipo, string? nome, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Quantos documentos do tipo existem no prédio (limites de RN40/RN41). Com <paramref name="deUtc"/>
    /// e <paramref name="ateUtc"/>, conta só os enviados no intervalo <c>[deUtc, ateUtc)</c>.
    /// </summary>
    Task<int> ContarAsync(
        int predioId, TipoDocumentoPredio tipo, DateTime? deUtc = null, DateTime? ateUtc = null, CancellationToken ct = default);

    Task AdicionarAsync(Documento documento, CancellationToken ct = default);
    Task RemoverAsync(Documento documento, CancellationToken ct = default);
}
