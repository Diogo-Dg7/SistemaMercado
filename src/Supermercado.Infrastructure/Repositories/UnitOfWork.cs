using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Infrastructure.Data;
using Supermercado.Infrastructure.Data.Configurations;

namespace Supermercado.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    // SQL Server: 2601 = índice único violado; 2627 = constraint UNIQUE/PRIMARY KEY violada.
    private const int ViolacaoDeIndiceUnico = 2601;
    private const int ViolacaoDeConstraintUnica = 2627;

    private readonly SupermercadoDbContext _context;

    public UnitOfWork(SupermercadoDbContext context)
    {
        _context = context;
    }

    public async Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ViolaCodigoBarrasUnico(ex))
        {
            // Detalhe do SQL Server não vaza: o restante do sistema só conhece a regra de negócio.
            throw new DomainException("Já existe um produto com este código de barras.", ex);
        }
    }

    private static bool ViolaCodigoBarrasUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: ViolacaoDeIndiceUnico or ViolacaoDeConstraintUnica } sql
        && sql.Message.Contains(ProdutoConfiguration.IndiceCodigoBarrasUnico, StringComparison.OrdinalIgnoreCase);
}
