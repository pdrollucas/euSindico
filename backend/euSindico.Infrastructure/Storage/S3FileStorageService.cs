using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using euSindico.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace euSindico.Infrastructure.Storage;

public class S3FileStorageService(IAmazonS3 s3, IOptions<S3Options> options) : IFileStorageService
{
    private readonly string _bucket = options.Value.BucketName;

    public async Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(conteudo);

        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucket,
                Key = chave,
                InputStream = stream,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            },
            ct);
    }

    public async Task<Stream?> LerAsync(string chave, CancellationToken ct = default)
    {
        try
        {
            using var resposta = await s3.GetObjectAsync(_bucket, chave, ct);

            // Copia para memória: a resposta do SDK precisa ser descartada aqui, e o consumidor
            // (o controller) só devolve o stream ao cliente HTTP.
            var memoria = new MemoryStream();
            await resposta.ResponseStream.CopyToAsync(memoria, ct);
            memoria.Position = 0;
            return memoria;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // O S3 responde 204 também para chave inexistente, então "não falha se não existir" é nativo.
    public async Task RemoverAsync(string chave, CancellationToken ct = default) =>
        await s3.DeleteObjectAsync(_bucket, chave, ct);

    public async Task RemoverPrefixoAsync(string prefixo, CancellationToken ct = default)
    {
        // Trava contra apagar mais do que o pretendido: "relatorios/1" casaria com "relatorios/10/...",
        // e prefixo vazio casaria com o bucket inteiro.
        if (string.IsNullOrWhiteSpace(prefixo) || !prefixo.EndsWith('/'))
        {
            throw new ArgumentException("O prefixo deve ser não vazio e terminar em '/'.", nameof(prefixo));
        }

        string? token = null;
        do
        {
            var pagina = await s3.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = _bucket, Prefix = prefixo, ContinuationToken = token },
                ct);

            // O DeleteObjects aceita até 1000 chaves por chamada, o mesmo máximo de uma página da listagem.
            var objetos = pagina.S3Objects?.Select(o => new KeyVersion { Key = o.Key }).ToList() ?? [];
            if (objetos.Count > 0)
            {
                var resposta = await s3.DeleteObjectsAsync(
                    new DeleteObjectsRequest { BucketName = _bucket, Objects = objetos, Quiet = true },
                    ct);

                // Falha parcial vem na resposta, não como exceção: sem isso ela passaria despercebida.
                if (resposta.DeleteErrors is { Count: > 0 } erros)
                {
                    throw new InvalidOperationException(
                        $"Não foi possível apagar {erros.Count} objeto(s) sob '{prefixo}' (ex.: {erros[0].Key}: {erros[0].Code}).");
                }
            }

            token = pagina.IsTruncated == true ? pagina.NextContinuationToken : null;
        }
        while (token is not null);
    }
}
