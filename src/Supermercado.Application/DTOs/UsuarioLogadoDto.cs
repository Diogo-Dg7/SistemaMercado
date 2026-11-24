using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

/// <summary>Quem está autenticado, conforme as claims do token.</summary>
public sealed record UsuarioLogadoDto(Guid Id, string Nome, string Email, PerfilUsuario Perfil);
