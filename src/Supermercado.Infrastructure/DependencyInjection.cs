using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Interfaces;
using Supermercado.Infrastructure.Data;
using Supermercado.Infrastructure.Repositories;
using Supermercado.Infrastructure.Services;

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
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Autenticação: o hasher não guarda estado e o emissor de token só lê configuração já validada.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // A API recusa subir se Jwt:Chave estiver ausente ou com menos de 32 caracteres.
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Secao))
            .ValidateOnStart();

        return services;
    }
}
