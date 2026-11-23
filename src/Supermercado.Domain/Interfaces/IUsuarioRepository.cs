using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Interfaces;

public interface IUsuarioRepository : IReadRepository<Usuario>, IWriteRepository<Usuario>
{
    /// <summary>Consulta somente leitura (sem rastreamento), usada no login.</summary>
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Obtém o usuário COM rastreamento, para alteração (perfil, ativação).</summary>
    Task<Usuario?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Indica se já existe algum usuário (usado para criar o primeiro Gerente).</summary>
    Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default);
}
