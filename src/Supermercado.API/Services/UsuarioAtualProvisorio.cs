using Supermercado.Application.Interfaces;
using Supermercado.Domain.Enums;

namespace Supermercado.API.Services;

/// <summary>
/// PROVISÓRIO: enquanto a autenticação JWT não existe, todo usuário é tratado como Operador
/// (o perfil de menor privilégio: não pode vender abaixo do custo).
/// Deve ser substituído por uma implementação que leia o perfil do token JWT.
/// </summary>
public class UsuarioAtualProvisorio : IUsuarioAtual
{
    public PerfilUsuario Perfil => PerfilUsuario.Operador;
}
