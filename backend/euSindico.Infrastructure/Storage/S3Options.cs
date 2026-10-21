namespace euSindico.Infrastructure.Storage;

public class S3Options
{
    public const string SectionName = "S3";

    /// <summary>Bucket privado: nenhum objeto é exposto por URL pública (RFC 6.1, RNF09).</summary>
    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-2";
}
