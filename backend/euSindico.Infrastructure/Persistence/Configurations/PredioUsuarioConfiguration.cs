using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class PredioUsuarioConfiguration : IEntityTypeConfiguration<PredioUsuario>
{
    public void Configure(EntityTypeBuilder<PredioUsuario> builder)
    {
        builder.ToTable("predio_usuarios");

        builder.HasKey(pu => pu.Id);

        builder.Property(pu => pu.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(pu => pu.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        // Valores fixos e explícitos no enum (ver PapelPredio) — nunca renumerar um papel
        // existente, só adicionar novos valores.
        builder.Property(pu => pu.Papel)
            .HasColumnName("papel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(pu => pu.ConvidadoPorUsuarioId)
            .HasColumnName("convidado_por_usuario_id");

        builder.Property(pu => pu.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasOne(pu => pu.Predio)
            .WithMany(p => p.Membros)
            .HasForeignKey(pu => pu.PredioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pu => pu.Usuario)
            .WithMany(u => u.PrediosVinculados)
            .HasForeignKey(pu => pu.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quem convidou (self-referencing para usuarios) — sem navegação dedicada, só a FK.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(pu => pu.ConvidadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um usuário tem no máximo um papel por prédio (RN18).
        builder.HasIndex(pu => new { pu.PredioId, pu.UsuarioId }).IsUnique();
    }
}
