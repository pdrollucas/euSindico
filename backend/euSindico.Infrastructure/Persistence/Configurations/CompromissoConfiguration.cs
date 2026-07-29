using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class CompromissoConfiguration : IEntityTypeConfiguration<Compromisso>
{
    public void Configure(EntityTypeBuilder<Compromisso> builder)
    {
        builder.ToTable("compromissos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.DataCompromisso)
            .HasColumnName("data_compromisso")
            .IsRequired();

        builder.Property(c => c.HorarioCompromisso)
            .HasColumnName("horario_compromisso")
            .IsRequired();

        builder.Property(c => c.NomePrestadorServico)
            .HasColumnName("nome_prestador_servico")
            .HasMaxLength(150);

        builder.Property(c => c.Detalhes)
            .HasColumnName("detalhes")
            .HasColumnType("text");

        builder.Property(c => c.Concluido)
            .HasColumnName("concluido")
            .IsRequired();

        builder.Property(c => c.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasOne(c => c.Predio)
            .WithMany(p => p.Compromissos)
            .HasForeignKey(c => c.PredioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cobre a listagem por prédio já ordenada cronologicamente (RN14), incluindo o filtro "hoje".
        builder.HasIndex(c => new { c.PredioId, c.DataCompromisso, c.HorarioCompromisso });
    }
}
