namespace Supermercado.Application.Interfaces;

/// <summary>Cálculo e conferência de hash de senha. A implementação (PBKDF2) fica na Infrastructure.</summary>
public interface IPasswordHasher
{
    /// <summary>Gera o hash (com sal) da senha. Cada chamada produz um resultado diferente.</summary>
    string Gerar(string senha);

    /// <returns><c>true</c> se a senha corresponde ao hash.</returns>
    bool Verificar(string senhaHash, string senha);
}
