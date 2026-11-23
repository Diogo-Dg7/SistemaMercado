using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

public sealed record CriarUsuarioRequest(string Nome, string Email, string Senha, PerfilUsuario Perfil);
