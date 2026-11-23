using Supermercado.Domain.Entities;

namespace Supermercado.Domain.Interfaces;

/// <summary>Contrato apenas de leitura: quem só consulta depende só disto.</summary>
public interface IReadRepository<T> where T : Entity
{
    Task<T?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListarAsync(CancellationToken cancellationToken = default);
}
