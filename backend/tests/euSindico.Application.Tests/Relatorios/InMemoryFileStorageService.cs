using euSindico.Application.Common.Interfaces;

namespace euSindico.Application.Tests.Relatorios;

/// <summary>Fake em memória de <see cref="IFileStorageService"/> — os testes não dependem do S3.</summary>
public class InMemoryFileStorageService : IFileStorageService
{
    public Dictionary<string, (byte[] Conteudo, string ContentType)> Arquivos { get; } = [];

    public bool FalharAoSalvar { get; set; }

    public Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken ct = default)
    {
        if (FalharAoSalvar)
        {
            throw new IOException("Falha simulada no armazenamento.");
        }

        Arquivos[chave] = (conteudo, contentType);
        return Task.CompletedTask;
    }

    public Task<Stream?> LerAsync(string chave, CancellationToken ct = default) =>
        Task.FromResult<Stream?>(Arquivos.TryGetValue(chave, out var arquivo) ? new MemoryStream(arquivo.Conteudo) : null);

    public Task RemoverAsync(string chave, CancellationToken ct = default)
    {
        Arquivos.Remove(chave);
        return Task.CompletedTask;
    }
}
