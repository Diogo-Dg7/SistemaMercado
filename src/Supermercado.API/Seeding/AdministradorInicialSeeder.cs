using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Interfaces;

namespace Supermercado.API.Seeding;

/// <summary>
/// Cria o primeiro Gerente na inicialização, SÓ quando não existe nenhum usuário. E-mail e senha vêm de
/// "AdminInicial:Email" e "AdminInicial:Senha" (dotnet user-secrets / variáveis de ambiente): nenhuma senha
/// fica em migration ou no código. Reaproveita o UsuarioService, então valem as mesmas validações e o hash.
/// </summary>
public sealed class AdministradorInicialSeeder : IHostedService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly IConfiguration _configuracao;
    private readonly ILogger<AdministradorInicialSeeder> _logger;

    public AdministradorInicialSeeder(
        IServiceScopeFactory escopos, IConfiguration configuracao, ILogger<AdministradorInicialSeeder> logger)
    {
        _escopos = escopos;
        _configuracao = configuracao;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var escopo = _escopos.CreateAsyncScope();

            if (await escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>().ExisteAlgumAsync(cancellationToken))
                return;

            var email = _configuracao["AdminInicial:Email"];
            var senha = _configuracao["AdminInicial:Senha"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                _logger.LogWarning(
                    "Não há usuários e AdminInicial:Email / AdminInicial:Senha não estão configurados: " +
                    "ninguém conseguirá fazer login. Configure-os com dotnet user-secrets (veja o README).");
                return;
            }

            var nome = _configuracao["AdminInicial:Nome"] is { Length: > 0 } n ? n : "Administrador";

            await escopo.ServiceProvider.GetRequiredService<IUsuarioService>().CriarAsync(
                new CriarUsuarioRequest(nome, email, senha, PerfilUsuario.Gerente), cancellationToken);

            _logger.LogInformation("Gerente inicial criado: {Email}.", email.Trim().ToLowerInvariant());
        }
        catch (Exception ex)
        {
            // Não derruba a API (ex.: banco ainda sem migration), mas o erro fica registrado. A senha nunca é logada.
            _logger.LogError(ex, "Não foi possível criar o Gerente inicial.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
