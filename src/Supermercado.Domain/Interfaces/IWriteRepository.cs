using Supermercado.Domain.Entities;

namespace Supermercado.Domain.Interfaces;

/// <summary>Contrato apenas de escrita. A persistência efetiva é confirmada pelo IUnitOfWork.</summary>
public interface IWriteRepository<T> where T : Entity
{
    Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default);

    void Atualizar(T entidade);

    void Remover(T entidade);
}
