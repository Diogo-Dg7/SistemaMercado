using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;
using Supermercado.Infrastructure.Data;

namespace Supermercado.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly SupermercadoDbContext _context;

    public ProdutoRepository(SupermercadoDbContext context)
    {
        _context = context;
    }

    // Leituras: sem rastreamento. Sempre carregam a Categoria, pois o Produto a exige (nunca nula).
    private IQueryable<Produto> Consulta() =>
        _context.Produtos.AsNoTracking().Include(p => p.Categoria);

    public Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Consulta().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Produto>> ListarAsync(CancellationToken cancellationToken = default) =>
        await Consulta().OrderBy(p => p.Nome).ToListAsync(cancellationToken);

    public Task<Produto?> ObterPorCodigoBarrasAsync(
        CodigoBarras codigoBarras, CancellationToken cancellationToken = default) =>
        Consulta().FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras, cancellationToken);

    public async Task<IReadOnlyList<Produto>> ListarAbaixoDoEstoqueMinimoAsync(
        CancellationToken cancellationToken = default) =>
        await Consulta()
            .Where(p => p.Estoque < p.EstoqueMinimo) // mesma regra de Produto.EstaAbaixoDoEstoqueMinimo
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        // A Categoria já existe no banco. Sem isto, o Add percorreria o grafo e tentaria inseri-la de novo.
        _context.AnexarExistente(produto.Categoria);

        await _context.Produtos.AddAsync(produto, cancellationToken);
    }

    // As leituras não rastreiam: marca só o produto (sem percorrer a Categoria).
    public void Atualizar(Produto produto) => _context.MarcarComoModificada(produto);

    public void Remover(Produto produto) => _context.MarcarComoRemovida(produto);
}
