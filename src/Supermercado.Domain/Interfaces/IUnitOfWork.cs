namespace Supermercado.Domain.Interfaces;

/// <summary>
/// Confirma, em uma única transação, as alterações feitas pelos repositórios.
/// </summary>
public interface IUnitOfWork
{
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
