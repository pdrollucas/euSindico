using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace euSindico.Infrastructure.Persistence.Configurations;

public class TipoDocumentoConfiguration : IEntityTypeConfiguration<TipoDocumento>
{
    public void Configure(EntityTypeBuilder<TipoDocumento> builder)
    {
        builder.ToTable("tipo_documento");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome)
            .HasColumnName("nome")
            .HasMaxLength(50)
            .IsRequired();

        // Valores fixos definidos no RFC (seção 5.2.2): "Atas" e "Normas".
        builder.HasData(
            new { Id = 1, Nome = "Atas" },
            new { Id = 2, Nome = "Normas" });
    }
}
