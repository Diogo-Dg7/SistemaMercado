using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;
using Supermercado.Infrastructure.Data;

namespace Supermercado.API.IntegrationTests.Repositories;

[Collection(BancoCollection.Nome)]
public class ProdutoRepositoryTests : IntegracaoTestBase
{
    public ProdutoRepositoryTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    // ---------- Salvar e buscar ----------

    [Fact]
    public async Task Salvar_e_buscar_por_id_deve_preservar_todos_os_campos()
    {
        var categoria = await SalvarCategoriaAsync("Hortifruti");
        var original = await SalvarProdutoAsync(categoria, c => NovoProduto(
            c, nome: "Banana prata", custo: 3.50m, venda: 5.99m,
            unidade: UnidadeMedida.Quilograma, estoqueMinimo: 2.5m, estoque: 12.345m));

        using var escopo = Banco.NovoEscopo();
        var lido = await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>().ObterPorIdAsync(original.Id);

        lido.Should().NotBeNull();
        lido!.Id.Should().Be(original.Id);
        lido.Nome.Should().Be("Banana prata");
        lido.CodigoBarras.Should().Be(original.CodigoBarras);
        lido.PrecoCusto.Should().Be(new Dinheiro(3.50m));
        lido.PrecoVenda.Should().Be(new Dinheiro(5.99m));
        lido.UnidadeMedida.Should().Be(UnidadeMedida.Quilograma);
        lido.Estoque.Should().Be(12.345m);
        lido.EstoqueMinimo.Should().Be(2.5m);
        lido.CategoriaId.Should().Be(categoria.Id);
        lido.Categoria.Nome.Should().Be("Hortifruti");
        lido.CriadoEm.Should().BeCloseTo(original.CriadoEm, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Salvar_produto_nao_deve_duplicar_nem_alterar_a_categoria()
    {
        var categoria = await SalvarCategoriaAsync();
        await SalvarProdutoAsync(categoria, c => NovoProduto(c));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Suco"));

        using var escopo = Banco.NovoEscopo();
        var categorias = await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>().ListarAsync();

        categorias.Should().ContainSingle().Which.Id.Should().Be(categoria.Id);
    }

    [Fact]
    public async Task Buscar_por_id_inexistente_deve_retornar_nulo()
    {
        using var escopo = Banco.NovoEscopo();

        var lido = await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>().ObterPorIdAsync(Guid.NewGuid());

        lido.Should().BeNull();
    }

    [Fact]
    public async Task Buscar_por_codigo_de_barras_deve_encontrar_o_produto()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await SalvarProdutoAsync(categoria, c => NovoProduto(c));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Outro"));

        using var escopo = Banco.NovoEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();

        var encontrado = await repositorio.ObterPorCodigoBarrasAsync(produto.CodigoBarras);
        var naoEncontrado = await repositorio.ObterPorCodigoBarrasAsync(new CodigoBarras(GeradorCodigoBarras.Proximo()));

        encontrado.Should().NotBeNull();
        encontrado!.Id.Should().Be(produto.Id);
        encontrado.Categoria.Should().NotBeNull();
        naoEncontrado.Should().BeNull();
    }

    [Fact]
    public async Task Listar_deve_retornar_todos_ordenados_por_nome()
    {
        var categoria = await SalvarCategoriaAsync();
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Leite"));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Arroz"));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Feijão"));

        using var escopo = Banco.NovoEscopo();
        var produtos = await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>().ListarAsync();

        produtos.Select(p => p.Nome).Should().Equal("Arroz", "Feijão", "Leite");
        produtos.Should().OnlyContain(p => p.Categoria != null && p.Categoria.Nome == "Bebidas");
    }

    // ---------- Estoque mínimo ----------

    [Fact]
    public async Task Listar_abaixo_do_estoque_minimo_deve_retornar_somente_os_produtos_em_falta()
    {
        var categoria = await SalvarCategoriaAsync();
        var abaixo = await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Abaixo", estoqueMinimo: 10m, estoque: 9m));
        var zerado = await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Zerado", estoqueMinimo: 1m, estoque: 0m));
        var fracionado = await SalvarProdutoAsync(categoria, c => NovoProduto(
            c, nome: "Fracionado", unidade: UnidadeMedida.Quilograma, estoqueMinimo: 5m, estoque: 4.999m));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Igual ao mínimo", estoqueMinimo: 10m, estoque: 10m));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Acima", estoqueMinimo: 10m, estoque: 50m));
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Sem mínimo", estoqueMinimo: 0m, estoque: 0m));

        using var escopo = Banco.NovoEscopo();
        var emFalta = await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>()
            .ListarAbaixoDoEstoqueMinimoAsync();

        emFalta.Select(p => p.Id).Should().BeEquivalentTo(new[] { abaixo.Id, zerado.Id, fracionado.Id });
        emFalta.Select(p => p.Nome).Should().Equal("Abaixo", "Fracionado", "Zerado"); // ordenado por nome
        emFalta.Should().OnlyContain(p => p.EstaAbaixoDoEstoqueMinimo());
    }

    // ---------- Código de barras duplicado ----------

    [Fact]
    public async Task Codigo_de_barras_duplicado_deve_lancar_DomainException_e_nao_a_excecao_do_sql_server()
    {
        var categoria = await SalvarCategoriaAsync();
        var codigo = GeradorCodigoBarras.Proximo();
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Original", codigoBarras: codigo));

        var act = () => SalvarProdutoAsync(categoria, c => NovoProduto(c, nome: "Duplicado", codigoBarras: codigo));

        var excecao = await act.Should().ThrowAsync<DomainException>();
        excecao.Which.Message.Should().Contain("código de barras");
        excecao.Which.InnerException.Should().BeOfType<DbUpdateException>();

        using var escopo = Banco.NovoEscopo();
        var produtos = await escopo.ServiceProvider.GetRequiredService<IProdutoRepository>().ListarAsync();
        produtos.Should().ContainSingle().Which.Nome.Should().Be("Original");
    }

    // ---------- Atualizar e remover ----------

    [Fact]
    public async Task Atualizar_deve_persistir_as_alteracoes_feitas_pelo_dominio()
    {
        var categoria = await SalvarCategoriaAsync();
        var original = await SalvarProdutoAsync(categoria, c => NovoProduto(c, custo: 5m, venda: 8m, estoque: 20m));

        using (var escopo = Banco.NovoEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();
            var produto = (await repositorio.ObterPorIdAsync(original.Id))!;

            produto.AtualizarPreco(new Dinheiro(10m), new Dinheiro(9m), PerfilUsuario.Gerente);
            produto.BaixarEstoque(7m);
            repositorio.Atualizar(produto);

            // Só o produto deve ser tocado; a Categoria (já existente) não pode ser reescrita.
            var contexto = escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>();
            contexto.ChangeTracker.Entries<Categoria>().Should().NotContain(
                e => e.State == EntityState.Modified || e.State == EntityState.Added);

            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SalvarAlteracoesAsync();
        }

        using var leitura = Banco.NovoEscopo();
        var atualizado = (await leitura.ServiceProvider.GetRequiredService<IProdutoRepository>()
            .ObterPorIdAsync(original.Id))!;

        atualizado.PrecoCusto.Should().Be(new Dinheiro(10m));
        atualizado.PrecoVenda.Should().Be(new Dinheiro(9m));
        atualizado.Estoque.Should().Be(13m);
    }

    [Fact]
    public async Task Atualizar_o_mesmo_produto_duas_vezes_no_mesmo_escopo_deve_persistir_as_duas_alteracoes()
    {
        var categoria = await SalvarCategoriaAsync();
        var original = await SalvarProdutoAsync(categoria, c => NovoProduto(c, estoque: 20m));

        using (var escopo = Banco.NovoEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();
            var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Cada leitura devolve uma instância nova (sem rastreamento), com o mesmo Id.
            var primeira = (await repositorio.ObterPorIdAsync(original.Id))!;
            primeira.BaixarEstoque(5m);
            repositorio.Atualizar(primeira);
            await unitOfWork.SalvarAlteracoesAsync();

            var segunda = (await repositorio.ObterPorIdAsync(original.Id))!;
            segunda.Should().NotBeSameAs(primeira);
            segunda.BaixarEstoque(3m);
            repositorio.Atualizar(segunda);
            await unitOfWork.SalvarAlteracoesAsync();
        }

        using var leitura = Banco.NovoEscopo();
        (await leitura.ServiceProvider.GetRequiredService<IProdutoRepository>().ObterPorIdAsync(original.Id))!
            .Estoque.Should().Be(12m);
    }

    [Fact]
    public async Task Adicionar_produto_com_categoria_ja_rastreada_no_mesmo_escopo_deve_funcionar()
    {
        Guid produtoId;

        using (var escopo = Banco.NovoEscopo())
        {
            var categorias = escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>();
            var produtos = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();
            var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var nova = new Categoria("Padaria");
            await categorias.AdicionarAsync(nova); // fica rastreada pelo contexto
            await unitOfWork.SalvarAlteracoesAsync();

            var lida = (await categorias.ObterPorIdAsync(nova.Id))!; // outra instância, mesmo Id
            lida.Should().NotBeSameAs(nova);

            var produto = NovoProduto(lida);
            produtoId = produto.Id;
            await produtos.AdicionarAsync(produto);
            await unitOfWork.SalvarAlteracoesAsync();
        }

        using var leitura = Banco.NovoEscopo();
        (await leitura.ServiceProvider.GetRequiredService<IProdutoRepository>().ObterPorIdAsync(produtoId))!
            .Categoria.Nome.Should().Be("Padaria");
        (await leitura.ServiceProvider.GetRequiredService<ICategoriaRepository>().ListarAsync())
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Remover_deve_apagar_o_produto()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await SalvarProdutoAsync(categoria, c => NovoProduto(c));

        using (var escopo = Banco.NovoEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();
            repositorio.Remover((await repositorio.ObterPorIdAsync(produto.Id))!);
            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SalvarAlteracoesAsync();
        }

        using var leitura = Banco.NovoEscopo();
        (await leitura.ServiceProvider.GetRequiredService<IProdutoRepository>().ObterPorIdAsync(produto.Id))
            .Should().BeNull();
    }

    // ---------- Leitura sem rastreamento e esquema ----------

    [Fact]
    public async Task Consultas_de_leitura_nao_devem_rastrear_entidades()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await SalvarProdutoAsync(categoria, c => NovoProduto(c, estoqueMinimo: 100m, estoque: 1m));

        using var escopo = Banco.NovoEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();

        await repositorio.ObterPorIdAsync(produto.Id);
        await repositorio.ObterPorCodigoBarrasAsync(produto.CodigoBarras);
        await repositorio.ListarAsync();
        await repositorio.ListarAbaixoDoEstoqueMinimoAsync();

        escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Esquema_do_banco_deve_seguir_os_mapeamentos()
    {
        var categoria = await SalvarCategoriaAsync();
        await SalvarProdutoAsync(categoria, c => NovoProduto(c, unidade: UnidadeMedida.Litro));

        using var escopo = Banco.NovoEscopo();
        var contexto = escopo.ServiceProvider.GetRequiredService<SupermercadoDbContext>();

        var colunas = await contexto.Database.SqlQueryRaw<ColunaDoBanco>(
            """
            SELECT COLUMN_NAME AS Nome, DATA_TYPE AS Tipo,
                   CAST(ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS int) AS Tamanho,
                   CAST(ISNULL(NUMERIC_PRECISION, 0) AS int) AS Precisao,
                   CAST(ISNULL(NUMERIC_SCALE, 0) AS int) AS Escala
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Produtos'
            """).ToListAsync();

        ColunaDoBanco Coluna(string nome) => colunas.Single(c => c.Nome == nome);

        Coluna("PrecoCusto").Should().Match<ColunaDoBanco>(c => c.Tipo == "decimal" && c.Precisao == 18 && c.Escala == 2);
        Coluna("PrecoVenda").Should().Match<ColunaDoBanco>(c => c.Tipo == "decimal" && c.Precisao == 18 && c.Escala == 2);
        Coluna("Estoque").Should().Match<ColunaDoBanco>(c => c.Tipo == "decimal" && c.Precisao == 18 && c.Escala == 3);
        Coluna("EstoqueMinimo").Should().Match<ColunaDoBanco>(c => c.Tipo == "decimal" && c.Precisao == 18 && c.Escala == 3);
        Coluna("CodigoBarras").Should().Match<ColunaDoBanco>(c => c.Tipo == "varchar" && c.Tamanho == 14);

        var unidadeSalva = await contexto.Database
            .SqlQueryRaw<string>("SELECT UnidadeMedida AS Value FROM Produtos").SingleAsync();
        unidadeSalva.Should().Be("Litro");

        var indiceUnico = await contexto.Database.SqlQueryRaw<int>(
            "SELECT CAST(is_unique AS int) AS Value FROM sys.indexes WHERE name = 'IX_Produtos_CodigoBarras'").SingleAsync();
        indiceUnico.Should().Be(1);
    }

    public sealed record ColunaDoBanco(string Nome, string Tipo, int Tamanho, int Precisao, int Escala);
}
