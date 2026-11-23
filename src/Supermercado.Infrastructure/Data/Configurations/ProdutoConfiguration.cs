using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermercado.Domain.Entities;
using Supermercado.Infrastructure.Data.Converters;

namespace Supermercado.Infrastructure.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    /// <summary>Nome do índice único; o UnitOfWork o usa para reconhecer a violação de código de barras.</summary>
    public const string IndiceCodigoBarrasUnico = "IX_Produtos_CodigoBarras";

    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .IsRequired()
            .HasMaxLength(Produto.NomeTamanhoMaximo);

        builder.Property(p => p.CodigoBarras)
            .HasConversion<CodigoBarrasConverter>()
            .HasMaxLength(14)
            .IsUnicode(false) // varchar(14)
            .IsRequired();

        builder.HasIndex(p => p.CodigoBarras)
            .IsUnique()
            .HasDatabaseName(IndiceCodigoBarrasUnico);

        // PrecoCusto e PrecoVenda (Dinheiro -> decimal(18,2)) são configurados por convenção no DbContext.

        builder.Property(p => p.Estoque).HasPrecision(18, 3);
        builder.Property(p => p.EstoqueMinimo).HasPrecision(18, 3);

        builder.Property(p => p.UnidadeMedida)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .IsRequired();

        // Token de concorrência otimista (rowversion). Shadow property: existe só no banco e no EF,
        // o Domain não sabe dele. Evita o "lost update" (dois caixas gravando estoque sobre o mesmo valor lido).
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();

        builder.HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict); // não apaga produtos ao apagar a categoria
    }
}
