using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

public sealed record ProdutoDto(
    Guid Id,
    string Nome,
    string CodigoBarras,
    decimal PrecoCusto,
    decimal PrecoVenda,
    UnidadeMedida UnidadeMedida,
    decimal Estoque,
    decimal EstoqueMinimo,
    bool AbaixoDoEstoqueMinimo,
    Guid CategoriaId);
