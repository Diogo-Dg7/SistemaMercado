using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Interfaces;
using Supermercado.Infrastructure.Data;

namespace Supermercado.Infrastructure.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly SupermercadoDbContext _context;

    public CategoriaRepository(SupermercadoDbContext context)
    {
        _context = context;
    }

    public Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _context.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Categoria categoria, CancellationToken cancellationToken = default) =>
        await _context.Categorias.AddAsync(categoria, cancellationToken);

    public void Atualizar(Categoria categoria) => _context.MarcarComoModificada(categoria);

    public void Remover(Categoria categoria) => _context.MarcarComoRemovida(categoria);
}
