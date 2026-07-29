using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class CodigoRedefinicaoSenhaConfiguration : IEntityTypeConfiguration<CodigoRedefinicaoSenha>
{
    public void Configure(EntityTypeBuilder<CodigoRedefinicaoSenha> builder)
    {
        builder.ToTable("codigos_redefinicao_senha");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
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

        // Sem índice único em codigo_hash isolado (diferente do refresh_tokens.token_hash):
        // com só 6 caracteres, colisão entre usuários diferentes é estatisticamente plausível.
        builder.HasIndex(c => new { c.UsuarioId, c.CodigoHash });

        // Cobre o cooldown (busca do último código do usuário, ordenado por criado_em) e a
        // invalidação em massa (nova solicitação mata qualquer código ainda válido).
        builder.HasIndex(c => new { c.UsuarioId, c.CriadoEm });
        builder.HasIndex(c => new { c.UsuarioId, c.UsadoEm });
    }
}