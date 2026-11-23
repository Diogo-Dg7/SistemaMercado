using Supermercado.Domain.Entities;

namespace Supermercado.Domain.Interfaces;

/// <summary>
/// Contrato apenas de escrita. A persistência efetiva é confirmada pelo IUnitOfWork.
/// Não há "Atualizar": para alterar, obtenha a entidade com ObterParaAlteracaoAsync (rastreada),
/// chame os métodos do domínio e confirme com IUnitOfWork.SalvarAlteracoesAsync.
/// </summary>
public interface IWriteRepository<T> where T : Entity
{
    Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default);

    /// <summary>Remove uma entidade obtida com ObterParaAlteracaoAsync.</summary>
    void Remover(T entidade);
}
