using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;

namespace Supermercado.Application.Mappings;

public static class UsuarioMappings
{
    public static UsuarioDto ToDto(this Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email.Valor, usuario.Perfil, usuario.Ativo);
}
