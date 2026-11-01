using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class CodigoTrocaEmailConfiguration : IEntityTypeConfiguration<CodigoTrocaEmail>
{
    public void Configure(EntityTypeBuilder<CodigoTrocaEmail> builder)
    {
        builder.ToTable("codigos_troca_email");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(c => c.NovoEmail)
            .HasColumnName("novo_email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.CodigoHash)
            .HasColumnName("codigo_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(c => c.ExpiraEm)
            .HasColumnName("expira_em")
            .IsRequired();

        builder.Property(c => c.UsadoEm)
            .HasColumnName("usado_em");

        builder.HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mesma lógica de codigos_redefinicao_senha: sem índice único em codigo_hash (6 caracteres).
        builder.HasIndex(c => new { c.UsuarioId, c.CodigoHash });
        builder.HasIndex(c => new { c.UsuarioId, c.CriadoEm });
        builder.HasIndex(c => new { c.UsuarioId, c.UsadoEm });
    }
}
