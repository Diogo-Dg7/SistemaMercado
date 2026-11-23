using Supermercado.Domain.Enums;

namespace Supermercado.Application.Interfaces;

/// <summary>
/// Usuário autenticado na requisição atual. A implementação ficará na API (leitura do JWT).
/// </summary>
public interface IUsuarioAtual
{
    PerfilUsuario Perfil { get; }
}
