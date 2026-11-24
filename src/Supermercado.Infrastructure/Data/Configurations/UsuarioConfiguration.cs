using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermercado.Domain.Entities;
using Supermercado.Infrastructure.Data.Converters;

namespace Supermercado.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <summary>Nome do índice único; o UnitOfWork o usa para reconhecer a violação de e-mail duplicado.</summary>
    public const string IndiceEmailUnico = "IX_Usuarios_Email";

    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(Usuario.NomeTamanhoMaximo);

        builder.Property(u => u.Email)
            .HasConversion<EmailConverter>()
            .HasMaxLength(Supermercado.Domain.ValueObjects.Email.TamanhoMaximo)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName(IndiceEmailUnico);

        builder.Property(u => u.SenhaHash)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(Usuario.SenhaHashTamanhoMaximo);

        builder.Property(u => u.Perfil)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.Ativo)
            .IsRequired();

        builder.Property(u => u.CriadoEm)
            .IsRequired();
    }
}
