using Supermercado.Domain.Entities;

namespace Supermercado.Domain.Interfaces;

public interface ICategoriaRepository : IReadRepository<Categoria>, IWriteRepository<Categoria>
{
    /// <summary>
    /// Obtém a categoria COM rastreamento. Use ao criar um Produto (para que o contexto saiba que a
    /// categoria já existe) ou ao alterá-la.
    /// </summary>
    Task<Categoria?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken = default);
}
