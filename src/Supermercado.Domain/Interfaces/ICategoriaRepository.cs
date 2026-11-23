using Supermercado.Domain.Entities;

namespace Supermercado.Domain.Interfaces;

public interface ICategoriaRepository : IReadRepository<Categoria>, IWriteRepository<Categoria>
{
}
