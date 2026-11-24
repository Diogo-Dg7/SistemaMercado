using Microsoft.Extensions.Options;

namespace Supermercado.Infrastructure.Services;

/// <summary>
/// Seção "Jwt" da configuração. A Chave é SEGREDO: só em dotnet user-secrets (ou variável de ambiente
/// Jwt__Chave em produção), nunca no appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    public const string Secao = "Jwt";
    public const int TamanhoMinimoDaChave = 32;

    public string Issuer { get; set; } = "";

    public string Audience { get; set; } = "";

    public string Chave { get; set; } = "";
}

/// <summary>Faz a API recusar subir com configuração de JWT ausente ou fraca (ValidateOnStart).</summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            erros.Add("Jwt:Issuer não foi configurado.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            erros.Add("Jwt:Audience não foi configurado.");

        // Nunca inclui o valor da chave na mensagem.
        if (string.IsNullOrWhiteSpace(options.Chave))
            erros.Add("Jwt:Chave não foi configurada. Defina-a com dotnet user-secrets (veja o README).");
        else if (options.Chave.Length < JwtOptions.TamanhoMinimoDaChave)
            erros.Add($"Jwt:Chave deve ter pelo menos {JwtOptions.TamanhoMinimoDaChave} caracteres.");

        return erros.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(erros);
    }
}
