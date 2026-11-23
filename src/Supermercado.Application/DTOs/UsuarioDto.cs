using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

/// <summary>Nunca inclui o hash da senha.</summary>
public sealed record UsuarioDto(Guid Id, string Nome, string Email, PerfilUsuario Perfil, bool Ativo);
