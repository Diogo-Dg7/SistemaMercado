using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.ValueObjects;
using Supermercado.Infrastructure.Data;

namespace Supermercado.API.IntegrationTests.Infrastructure;

/// <summary>API real (Program) apontando para o banco de testes.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string? _chaveJwt;
    private readonly string _ambiente;
    private readonly string _adminEmail;
    private readonly string _adminSenha;

    /// <param name="chaveJwt">Chave de assinatura. <c>null</c> deixa a API sem chave (para testar a recusa de subir).</param>
    /// <param name="adminEmail">AdminInicial:Email. Vazio (padrão) = sem Gerente inicial.</param>
    /// <param name="adminSenha">AdminInicial:Senha.</param>
    public ApiFactory(
        string connectionString, string? chaveJwt, string ambiente = "Development",
        string adminEmail = "", string adminSenha = "")
    {
        _connectionString = connectionString;
        _chaveJwt = chaveJwt;
        _ambiente = ambiente;
        _adminEmail = adminEmail;
        _adminSenha = adminSenha;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_ambiente);
        builder.UseSetting("ConnectionStrings:Supermercado", _connectionString);
        builder.UseSetting("Jwt:Chave", _chaveJwt ?? "");

        // Por padrão os testes não dependem do Gerente inicial do ambiente de quem os executa.
        builder.UseSetting("AdminInicial:Email", _adminEmail);
        builder.UseSetting("AdminInicial:Senha", _adminSenha);
    }

    /// <summary>
    /// Token válido para o perfil, emitido pelo próprio ITokenService da API (mesma chave e claims do login),
    /// sem precisar de um usuário no banco: a validação do JWT é sem estado.
    /// </summary>
    public string CriarToken(PerfilUsuario perfil, string? email = null)
    {
        var usuario = new Usuario(
            $"Teste {perfil}", new Email(email ?? $"{perfil.ToString().ToLowerInvariant()}@teste.com"),
            "hash-irrelevante", perfil);

        return Services.GetRequiredService<ITokenService>().GerarToken(usuario).Token;
    }

    /// <summary>Cliente HTTP já autenticado com um token do perfil informado.</summary>
    public HttpClient CriarClienteAutenticado(PerfilUsuario perfil)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CriarToken(perfil));
        return cliente;
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
