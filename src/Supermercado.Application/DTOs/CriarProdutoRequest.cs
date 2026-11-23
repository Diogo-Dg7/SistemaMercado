using Supermercado.Domain.Enums;

namespace Supermercado.Application.DTOs;

public sealed record CriarProdutoRequest(
    string Nome,
    string CodigoBarras,
    decimal PrecoCusto,
    decimal PrecoVenda,
    Guid CategoriaId,
    UnidadeMedida UnidadeMedida,
    decimal EstoqueMinimo = 0,
    decimal EstoqueInicial = 0);
