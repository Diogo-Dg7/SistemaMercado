using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Interfaces;

public interface IProdutoRepository : IReadRepository<Produto>, IWriteRepository<Produto>
{
    /// <summary>
    /// Obtém o produto COM rastreamento, para alteração: as mudanças feitas pelos métodos do domínio são
    /// detectadas e gravadas por IUnitOfWork.SalvarAlteracoesAsync, com controle de concorrência.
    /// Várias chamadas com o mesmo id no mesmo escopo devolvem a mesma instância.
    /// </summary>
    Task<Produto?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Consulta somente leitura (sem rastreamento).</summary>
    Task<Produto?> ObterPorCodigoBarrasAsync(CodigoBarras codigoBarras, CancellationToken cancellationToken = default);

    /// <summary>Produtos com estoque abaixo do mínimo (alerta de reposição). Somente leitura.</summary>
    Task<IReadOnlyList<Produto>> ListarAbaixoDoEstoqueMinimoAsync(CancellationToken cancellationToken = default);
}
