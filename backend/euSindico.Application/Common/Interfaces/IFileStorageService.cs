namespace euSindico.Application.Common.Interfaces;

/// <summary>
/// Armazenamento de arquivos binários (relatórios em PDF; documentos no futuro). A Application só
/// conhece este contrato — a implementação concreta (AWS S3) fica na Infrastructure.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Grava o conteúdo sob a chave informada, sobrescrevendo qualquer objeto existente.</summary>
    Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken ct = default);

    /// <summary>Conteúdo da chave, ou <c>null</c> se não existir. Quem recebe o stream o descarta.</summary>
    Task<Stream?> LerAsync(string chave, CancellationToken ct = default);

    /// <summary>Remove o objeto; não falha se ele não existir.</summary>
    Task RemoverAsync(string chave, CancellationToken ct = default);

    /// <summary>
    /// Remove todos os objetos cujo nome começa com o prefixo. O prefixo é obrigatório e deve terminar
    /// em <c>/</c> (<see cref="ArgumentException"/> caso contrário): sem a barra, <c>relatorios/1</c>
    /// também apagaria <c>relatorios/10/...</c>, e um prefixo vazio apagaria tudo. Não falha se não
    /// houver objetos.
    /// </summary>
    Task RemoverPrefixoAsync(string prefixo, CancellationToken ct = default);
}
