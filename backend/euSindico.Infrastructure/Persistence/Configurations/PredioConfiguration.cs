using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class PredioConfiguration : IEntityTypeConfiguration<Predio>
{
    public void Configure(EntityTypeBuilder<Predio> builder)
    {
        builder.ToTable("predios");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .HasColumnName("nome")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Endereco)
            .HasColumnName("endereco")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(p => p.Excluido)
            .HasColumnName("excluido")
            .IsRequired();

        builder.Property(p => p.ExcluidoEm)
            .HasColumnName("excluido_em");

        builder.HasOne(p => p.Usuario)
            .WithMany(u => u.Predios)
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cobre a consulta mais comum: prédios de um usuário que não estão excluídos (RN02 + RN08).
        builder.HasIndex(p => new { p.UsuarioId, p.Excluido });
    }
}
