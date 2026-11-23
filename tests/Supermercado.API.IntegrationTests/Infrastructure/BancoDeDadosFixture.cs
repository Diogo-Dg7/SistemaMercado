using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Application;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Enums;
using Supermercado.Infrastructure;
using Supermercado.Infrastructure.Data;

namespace Supermercado.API.IntegrationTests.Infrastructure;

/// <summary>
/// Cria o banco de testes (EnsureDeleted + Migrate) uma vez no início da execução e o apaga no final.
/// A connection string vem de appsettings.Testes.json e pode ser sobrescrita pela variável de ambiente
/// ConnectionStrings__SupermercadoTestes.
/// </summary>
public sealed class BancoDeDadosFixture : IAsyncLifetime
{
    public const string NomeBancoDeTestes = "Supermercado_Testes";

    private readonly ServiceProvider _provider;

    public string ConnectionString { get; }

    public BancoDeDadosFixture()
    {
        var configuracao = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testes.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuracao.GetConnectionString("SupermercadoTestes")
            ?? throw new InvalidOperationException("Defina ConnectionStrings:SupermercadoTestes para os testes de integração.");

        ConnectionString = connectionString;

        // Trava de segurança: este fixture apaga o banco; nunca pode apontar para o banco real.
        var banco = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!string.Equals(banco, NomeBancoDeTestes, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Por segurança, os testes só rodam no banco '{NomeBancoDeTestes}', mas a connection string aponta para '{banco}'.");

        // Mesma ligação de DI usada pela API: AddApplication + AddInfrastructure.
        var configuracaoDaApi = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{Supermercado.Infrastructure.DependencyInjection.NomeConnectionString}"] = connectionString
            })
            .Build();

        var servicos = new ServiceCollection();
        servicos.AddApplication();
        servicos.AddInfrastructure(configuracaoDaApi);
        servicos.AddScoped<UsuarioAtualFalso>();
        servicos.AddScoped<IUsuarioAtual>(sp => sp.GetRequiredService<UsuarioAtualFalso>());

        _provider = servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public async Task InitializeAsync()
    {
        await using var escopo = _provider.CreateAsyncScope();
        var banco = escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>().Database;

        await banco.EnsureDeletedAsync();
        await banco.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var escopo = _provider.CreateAsyncScope())
        {
            await escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>().Database.EnsureDeletedAsync();
        }

        await _provider.DisposeAsync();
    }

    /// <summary>Escopo equivalente a uma requisição HTTP (um DbContext por escopo).</summary>
    public IServiceScope NovoEscopo(PerfilUsuario perfil = PerfilUsuario.Operador)
    {
        var escopo = _provider.CreateScope();
        escopo.ServiceProvider.GetRequiredService<UsuarioAtualFalso>().Perfil = perfil;
        return escopo;
    }

    public async Task LimparTabelasAsync()
    {
        using var escopo = _provider.CreateScope();
        await escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>().Database
            .ExecuteSqlRawAsync("DELETE FROM Produtos; DELETE FROM Categorias;");
    }

    private sealed class UsuarioAtualFalso : IUsuarioAtual
    {
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Operador;
    }
}
