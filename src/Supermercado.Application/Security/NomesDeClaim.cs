namespace Supermercado.Application.Security;

/// <summary>
/// Nomes das claims do token. A Infrastructure as escreve ao emitir o JWT e a API as lê ao validá-lo,
/// então ficam num só lugar.
/// </summary>
public static class NomesDeClaim
{
    public const string Id = "sub";
    public const string Nome = "name";
    public const string Email = "email";
    public const string Perfil = "role";
}
