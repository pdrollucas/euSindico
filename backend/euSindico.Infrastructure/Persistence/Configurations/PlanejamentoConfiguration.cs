using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class PlanejamentoConfiguration : IEntityTypeConfiguration<Planejamento>
{
    public void Configure(EntityTypeBuilder<Planejamento> builder)
    {
        builder.ToTable("planejamentos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.DataPlanejamento)
            .HasColumnName("data_planejamento");

        builder.Property(p => p.OrcamentoPrevisto)
            .HasColumnName("orcamento_previsto")
            .HasColumnType("decimal(10,2)");

        builder.Property(p => p.NomePrestadorServico)
            .HasColumnName("nome_prestador_servico")
            .HasMaxLength(150);

        builder.Property(p => p.Detalhes)
            .HasColumnName("detalhes")
            .HasColumnType("text");

        builder.Property(p => p.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasOne(p => p.Predio)
            .WithMany(pr => pr.Planejamentos)
            .HasForeignKey(p => p.PredioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
