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
}
