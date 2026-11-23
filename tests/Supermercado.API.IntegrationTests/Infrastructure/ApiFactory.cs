using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supermercado.Infrastructure.Data;

namespace Supermercado.API.IntegrationTests.Infrastructure;

/// <summary>API real (Program) apontando para o banco de testes.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _ambiente;

    public ApiFactory(string connectionString, string ambiente = "Development")
    {
        _connectionString = connectionString;
        _ambiente = ambiente;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_ambiente);
        builder.UseSetting("ConnectionStrings:Supermercado", _connectionString);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Trava de segurança: se a configuração não tiver sido substituída, a API estaria apontando
        // para o banco REAL (appsettings.json). Falha antes de qualquer teste escrever nele.
        using var escopo = host.Services.CreateScope();
        var connectionString = escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>()
            .Database.GetConnectionString();
        var banco = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        if (!string.Equals(banco, BancoDeDadosFixture.NomeBancoDeTestes, StringComparison.OrdinalIgnoreCase))
        {
            host.Dispose();
            throw new InvalidOperationException(
                $"A API dos testes aponta para o banco '{banco}', e não para '{BancoDeDadosFixture.NomeBancoDeTestes}'.");
        }

        return host;
    }
}

public static class JsonTeste
{
    /// <summary>Mesmo formato da API: camelCase e enums como texto.</summary>
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
