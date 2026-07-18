using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class RelatorioConfiguration : IEntityTypeConfiguration<Relatorio>
{
    public void Configure(EntityTypeBuilder<Relatorio> builder)
    {
        builder.ToTable("relatorios");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.MesReferencia)
            .HasColumnName("mes_referencia")
            .IsRequired();

        builder.Property(r => r.AnoReferencia)
            .HasColumnName("ano_referencia")
            .IsRequired();

        builder.Property(r => r.NomeArquivo)
            .HasColumnName("nome_arquivo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.UrlArquivo)
            .HasColumnName("url_arquivo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(r => r.GeradoEm)
            .HasColumnName("gerado_em")
            .IsRequired();

        builder.HasOne(r => r.Predio)
            .WithMany(p => p.Relatorios)
            .HasForeignKey(r => r.PredioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cobre a busca/geração de relatório por prédio + mês/ano (RF26-28), e lista por prédio já ordenada.
        builder.HasIndex(r => new { r.PredioId, r.AnoReferencia, r.MesReferencia });
    }
}
