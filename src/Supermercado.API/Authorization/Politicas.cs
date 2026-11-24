namespace Supermercado.API.Authorization;

/// <summary>Políticas de autorização por perfil.</summary>
public static class Politicas
{
    /// <summary>Só o perfil Gerente.</summary>
    public const string ApenasGerente = "ApenasGerente";

    /// <summary>Operador ou Gerente (qualquer usuário autenticado do sistema).</summary>
    public const string GerenteOuOperador = "GerenteOuOperador";
}
