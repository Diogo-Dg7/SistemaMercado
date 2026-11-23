using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;

namespace Supermercado.Application.Mappings;

public static class CategoriaMappings
{
    public static CategoriaDto ToDto(this Categoria categoria) =>
        new(categoria.Id, categoria.Nome);
}
