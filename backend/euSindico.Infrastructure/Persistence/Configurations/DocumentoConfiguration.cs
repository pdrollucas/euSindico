using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documentos");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.NomeArquivo)
            .HasColumnName("nome_arquivo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.UrlArquivo)
            .HasColumnName("url_arquivo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.TipoDocumentoId)
            .HasColumnName("tipo_documento_id")
            .IsRequired();

        builder.Property(d => d.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(d => d.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasOne(d => d.TipoDocumento)
            .WithMany(t => t.Documentos)
            .HasForeignKey(d => d.TipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Predio)
            .WithMany(p => p.Documentos)
            .HasForeignKey(d => d.PredioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cobre a navegação por Atas/Normas dentro da página do prédio (seção 4.1 do RFC).
        builder.HasIndex(d => new { d.PredioId, d.TipoDocumentoId });
    }
}
