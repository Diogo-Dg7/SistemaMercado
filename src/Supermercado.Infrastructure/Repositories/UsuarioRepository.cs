using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;
using Supermercado.Infrastructure.Data;

namespace Supermercado.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly SupermercadoDbContext _context;

    public UsuarioRepository(SupermercadoDbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _context.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync(cancellationToken);

    public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
        _context.Usuarios.AnyAsync(cancellationToken);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await _context.Usuarios.AddAsync(usuario, cancellationToken);

    public void Remover(Usuario usuario) => _context.Usuarios.Remove(usuario);
}
