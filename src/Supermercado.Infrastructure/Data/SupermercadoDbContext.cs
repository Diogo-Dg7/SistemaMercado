using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;
using Supermercado.Infrastructure.Data.Converters;

namespace Supermercado.Infrastructure.Data;

public class SupermercadoDbContext : DbContext
{
    public SupermercadoDbContext(DbContextOptions<SupermercadoDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Toda propriedade Dinheiro (hoje e nas próximas entidades) vira decimal(18,2).
        configurationBuilder.Properties<Dinheiro>()
            .HaveConversion<DinheiroConverter>()
            .HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupermercadoDbContext).Assembly);
    }
}
