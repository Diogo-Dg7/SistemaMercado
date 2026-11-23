using Microsoft.Extensions.DependencyInjection;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.API.IntegrationTests.Infrastructure;

/// <summary>Todos os testes que usam o banco compartilham a collection (rodam em série) e partem de tabelas vazias.</summary>
[CollectionDefinition(Nome)]
public class BancoCollection : ICollectionFixture<BancoDeDadosFixture>
{
    public const string Nome = "Banco de dados";
}

public abstract class IntegracaoTestBase : IAsyncLifetime
{
    protected IntegracaoTestBase(BancoDeDadosFixture banco)
    {
        Banco = banco;
    }

    protected BancoDeDadosFixture Banco { get; }

    public Task InitializeAsync() => Banco.LimparTabelasAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task<Categoria> SalvarCategoriaAsync(string nome = "Bebidas")
    {
        using var escopo = Banco.NovoEscopo();
        var categoria = new Categoria(nome);

        await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>().AdicionarAsync(categoria);
        await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SalvarAlteracoesAsync();

        return categoria;
    }

    /// <summary>
    /// Persiste o produto como a Application faz: a Categoria vem de uma leitura para alteração
    /// (rastreada) e o produto é adicionado em seguida.
    /// </summary>
    protected async Task<Produto> SalvarProdutoAsync(Categoria categoria, Func<Categoria, Produto> criar)
    {
        using var escopo = Banco.NovoEscopo();
        var categoriaLida = await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>()
            .ObterParaAlteracaoAsync(categoria.Id);
        var produto = criar(categoriaLida!);

        await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>().AdicionarAsync(produto);
        await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SalvarAlteracoesAsync();

        return produto;
    }

    protected static Produto NovoProduto(
        Categoria categoria,
        string nome = "Refrigerante 2L",
        string? codigoBarras = null,
        decimal custo = 5m,
        decimal venda = 8m,
        UnidadeMedida unidade = UnidadeMedida.Unidade,
        decimal estoqueMinimo = 10m,
        decimal estoque = 20m) =>
        new(
            nome,
            new CodigoBarras(codigoBarras ?? GeradorCodigoBarras.Proximo()),
            new Dinheiro(custo),
            new Dinheiro(venda),
            categoria,
            unidade,
            estoqueMinimo,
            estoque);
}
