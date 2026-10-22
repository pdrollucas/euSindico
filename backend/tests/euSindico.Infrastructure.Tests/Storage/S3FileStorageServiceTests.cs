using Amazon.S3;
using Amazon.S3.Model;
using euSindico.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Moq;

namespace euSindico.Infrastructure.Tests.Storage;

/// <summary>
/// Testa a lógica de <see cref="S3FileStorageService.RemoverPrefixoAsync"/> (travas, paginação e
/// falha parcial) com o cliente S3 mockado — não fala com a AWS.
/// </summary>
public class S3FileStorageServiceTests
{
    private const string Bucket = "bucket-teste";

    private readonly Mock<IAmazonS3> _s3 = new();
    private readonly S3FileStorageService _sut;

    public S3FileStorageServiceTests()
    {
        _sut = new S3FileStorageService(_s3.Object, Options.Create(new S3Options { BucketName = Bucket }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relatorios/10")]
    [InlineData("relatorios")]
    public async Task RemoverPrefixoAsync_com_prefixo_sem_barra_final_ou_vazio_lanca_e_nao_chama_o_S3(string prefixo)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RemoverPrefixoAsync(prefixo));

        _s3.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoverPrefixoAsync_lista_com_o_prefixo_exato_e_apaga_os_objetos_encontrados()
    {
        ListObjectsV2Request? listagem = null;
        DeleteObjectsRequest? exclusao = null;
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .Callback((ListObjectsV2Request r, CancellationToken _) => listagem = r)
            .ReturnsAsync(Pagina(["relatorios/10/a.pdf", "relatorios/10/b.pdf"]));
        _s3.Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .Callback((DeleteObjectsRequest r, CancellationToken _) => exclusao = r)
            .ReturnsAsync(new DeleteObjectsResponse());

        await _sut.RemoverPrefixoAsync("relatorios/10/");

        Assert.Equal(Bucket, listagem!.BucketName);
        Assert.Equal("relatorios/10/", listagem.Prefix);
        Assert.Equal(Bucket, exclusao!.BucketName);
        Assert.Equal(
            new[] { "relatorios/10/a.pdf", "relatorios/10/b.pdf" },
            exclusao.Objects.Select(o => o.Key));
    }

    [Fact]
    public async Task RemoverPrefixoAsync_sem_objetos_nao_chama_o_DeleteObjects()
    {
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Pagina([]));

        await _sut.RemoverPrefixoAsync("relatorios/10/");

        _s3.Verify(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoverPrefixoAsync_percorre_todas_as_paginas_da_listagem()
    {
        var tokensRecebidos = new List<string?>();
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListObjectsV2Request r, CancellationToken _) =>
            {
                tokensRecebidos.Add(r.ContinuationToken);
                return r.ContinuationToken is null
                    ? Pagina(["relatorios/10/a.pdf"], proximoToken: "pagina-2")
                    : Pagina(["relatorios/10/b.pdf"]);
            });
        var apagados = new List<string>();
        _s3.Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .Callback((DeleteObjectsRequest r, CancellationToken _) => apagados.AddRange(r.Objects.Select(o => o.Key)))
            .ReturnsAsync(new DeleteObjectsResponse());

        await _sut.RemoverPrefixoAsync("relatorios/10/");

        Assert.Equal(new[] { "relatorios/10/a.pdf", "relatorios/10/b.pdf" }, apagados);
        Assert.Equal(new string?[] { null, "pagina-2" }, tokensRecebidos);
    }

    [Fact]
    public async Task RemoverPrefixoAsync_com_falha_parcial_do_S3_lanca_excecao()
    {
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Pagina(["relatorios/10/a.pdf"]));
        _s3.Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectsResponse
            {
                DeleteErrors = [new DeleteError { Key = "relatorios/10/a.pdf", Code = "AccessDenied" }],
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RemoverPrefixoAsync("relatorios/10/"));

        Assert.Contains("AccessDenied", ex.Message);
    }

    private static ListObjectsV2Response Pagina(string[] chaves, string? proximoToken = null) =>
        new()
        {
            S3Objects = chaves.Select(k => new S3Object { Key = k }).ToList(),
            IsTruncated = proximoToken is not null,
            NextContinuationToken = proximoToken,
        };
}
