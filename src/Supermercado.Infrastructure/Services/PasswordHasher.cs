using Microsoft.AspNetCore.Identity;
using Supermercado.Application.Interfaces;

namespace Supermercado.Infrastructure.Services;

/// <summary>
/// Hash de senha com o PasswordHasher do Microsoft.Extensions.Identity.Core (PBKDF2 com sal aleatório
/// e contagem de iterações embutida no próprio hash). Não há criptografia implementada à mão.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    // O PasswordHasher do Identity pede um "usuário", mas não o usa; o hash só depende da senha.
    private static readonly object SemUsuario = new();

    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    public string Gerar(string senha) => _hasher.HashPassword(SemUsuario, senha);

    public bool Verificar(string senhaHash, string senha) =>
        _hasher.VerifyHashedPassword(SemUsuario, senhaHash, senha) != PasswordVerificationResult.Failed;
}
