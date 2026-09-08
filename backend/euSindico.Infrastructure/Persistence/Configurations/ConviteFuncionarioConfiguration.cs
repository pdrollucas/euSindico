using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class ConviteFuncionarioConfiguration : IEntityTypeConfiguration<ConviteFuncionario>
{
    public void Configure(EntityTypeBuilder<ConviteFuncionario> builder)
    {
        builder.ToTable("convites_funcionario");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PredioId)
            .HasColumnName("predio_id")
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(150)
            .IsRequired();

        // Valores fixos e explícitos no enum (ver PapelPredio) — nunca renumerar um papel
        // existente, só adicionar novos valores.
        builder.Property(c => c.Papel)
            .HasColumnName("papel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CriadoPorUsuarioId)
            .HasColumnName("criado_por_usuario_id")
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(c => c.ExpiraEm)
            .HasColumnName("expira_em")
            .IsRequired();

        builder.Property(c => c.UsadoEm)
            .HasColumnName("usado_em");

        builder.HasOne(c => c.Predio)
            .WithMany(p => p.Convites)
            .HasForeignKey(c => c.PredioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quem convidou (self-referencing para usuarios) — sem navegação dedicada, só a FK.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único: cada token, uma vez hasheado, precisa ser localizável rapidamente no aceite.
        builder.HasIndex(c => c.TokenHash).IsUnique();

        // Cobre a checagem de convite pendente por prédio+e-mail (RF29) — não é único porque
        // convites já expirados/usados para o mesmo e-mail continuam existindo na tabela.
        builder.HasIndex(c => new { c.PredioId, c.Email });
    }
}
