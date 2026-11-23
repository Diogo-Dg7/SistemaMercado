using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Domain.Interfaces;
using Supermercado.Infrastructure.Data;
using Supermercado.Infrastructure.Repositories;

namespace Supermercado.Infrastructure;

public static class DependencyInjection
{
    public const string NomeConnectionString = "Supermercado";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(NomeConnectionString);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"A connection string '{NomeConnectionString}' não foi configurada. " +
                "Defina-a em ConnectionStrings:Supermercado (appsettings.json ou user-secrets).");

        services.AddDbContext<SupermercadoDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
