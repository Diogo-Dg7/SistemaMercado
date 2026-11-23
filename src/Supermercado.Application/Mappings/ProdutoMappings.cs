using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;

namespace Supermercado.Application.Mappings;

public static class ProdutoMappings
{
    public static ProdutoDto ToDto(this Produto produto) =>
        new(
            produto.Id,
            produto.Nome,
            produto.CodigoBarras.Valor,
            produto.PrecoCusto.Valor,
            produto.PrecoVenda.Valor,
            produto.UnidadeMedida,
            produto.Estoque,
            produto.EstoqueMinimo,
            produto.EstaAbaixoDoEstoqueMinimo(),
            produto.CategoriaId);
}
