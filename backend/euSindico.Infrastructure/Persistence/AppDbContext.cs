using euSindico.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Predio> Predios => Set<Predio>();
    public DbSet<Compromisso> Compromissos => Set<Compromisso>();
    public DbSet<Planejamento> Planejamentos => Set<Planejamento>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Relatorio> Relatorios => Set<Relatorio>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
