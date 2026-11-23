using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

public sealed record LoginResponse(string Token, DateTime ExpiraEm, string Nome, PerfilUsuario Perfil);
