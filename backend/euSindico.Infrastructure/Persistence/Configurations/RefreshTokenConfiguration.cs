using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(rt => rt.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(rt => rt.ExpiraEm)
            .HasColumnName("expira_em")
            .IsRequired();

        builder.Property(rt => rt.RevogadoEm)
            .HasColumnName("revogado_em");

        builder.HasOne(rt => rt.Usuario)
            .WithMany()
            .HasForeignKey(rt => rt.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único: cada refresh token, uma vez hasheado, precisa ser localizável
        // rapidamente na renovação/logout (WHERE token_hash = ?).
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        // Cobre a revogação em massa (troca de senha) e a limpeza/consulta por usuário.
        builder.HasIndex(rt => new { rt.UsuarioId, rt.RevogadoEm });
    }
}
