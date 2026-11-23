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

    // Para alteração: COM rastreamento. Duas chamadas com o mesmo id no mesmo contexto devolvem a mesma
    // instância, e o UPDATE grava só o que mudou, protegido pelo token de concorrência (RowVersion).
    public Task<Produto?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Produtos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

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

    // A Categoria do produto deve vir de ICategoriaRepository.ObterParaAlteracaoAsync (rastreada);
    // uma instância desanexada seria inserida de novo pelo EF.
    public async Task AdicionarAsync(Produto produto, CancellationToken cancellationToken = default) =>
        await _context.Produtos.AddAsync(produto, cancellationToken);

    public void Remover(Produto produto) => _context.Produtos.Remove(produto);
}
