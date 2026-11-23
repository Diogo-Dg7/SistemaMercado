using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Interfaces;

public interface IProdutoRepository : IReadRepository<Produto>, IWriteRepository<Produto>
{
    Task<Produto?> ObterPorCodigoBarrasAsync(CodigoBarras codigoBarras, CancellationToken cancellationToken = default);

    /// <summary>Produtos com estoque abaixo do mínimo (alerta de reposição).</summary>
    Task<IReadOnlyList<Produto>> ListarAbaixoDoEstoqueMinimoAsync(CancellationToken cancellationToken = default);
}
