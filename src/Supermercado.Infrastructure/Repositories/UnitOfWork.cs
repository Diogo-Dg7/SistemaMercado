using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Supermercado.Application.Exceptions;
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

    // Índice único violado -> regra de negócio correspondente.
    private static readonly Dictionary<string, string> MensagensDeDuplicidade = new(StringComparer.OrdinalIgnoreCase)
    {
        [ProdutoConfiguration.IndiceCodigoBarrasUnico] = "Já existe um produto com este código de barras.",
        [UsuarioConfiguration.IndiceEmailUnico] = "Já existe um usuário com este e-mail.",
    };

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
        catch (DbUpdateConcurrencyException ex)
        {
            // Outra operação alterou o mesmo registro depois da nossa leitura (RowVersion não confere).
            throw new ConflitoDeConcorrenciaException(ex);
        }
        catch (DbUpdateException ex) when (ObterMensagemDeDuplicidade(ex) is not null)
        {
            // Detalhe do SQL Server não vaza: o restante do sistema só conhece a regra de negócio.
            throw new DomainException(ObterMensagemDeDuplicidade(ex)!, ex);
        }
    }

    private static string? ObterMensagemDeDuplicidade(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException { Number: ViolacaoDeIndiceUnico or ViolacaoDeConstraintUnica } sql)
            return null;

        return MensagensDeDuplicidade
            .FirstOrDefault(par => sql.Message.Contains(par.Key, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
