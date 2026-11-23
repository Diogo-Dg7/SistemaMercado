using System.Net;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Enums;

namespace Supermercado.API.IntegrationTests.Endpoints;

[Collection(BancoCollection.Nome)]
public class FluxoCompletoTests : ApiTestBase
{
    public FluxoCompletoTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    [Fact]
    public async Task Health_deve_responder_ok()
    {
        var resposta = await Client.GetAsync("/health");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Fluxo_completo_criar_categoria_criar_produto_movimentar_estoque_e_consultar()
    {
        // 1. Categoria: 201 + Location apontando para o GET {id}
        var respostaCategoria = await PostAsync("/api/categorias", new CriarCategoriaRequest("Hortifruti"));
        respostaCategoria.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoria = (await LerAsync<CategoriaDto>(respostaCategoria))!;
        categoria.Nome.Should().Be("Hortifruti");
        respostaCategoria.Headers.Location!.AbsolutePath.Should().Be($"/api/categorias/{categoria.Id}");

        // 2. Produto (por quilo): 201 + Location; enum trafega como texto
        var respostaProduto = await PostAsync("/api/produtos", new CriarProdutoRequest(
            "Banana prata", GeradorCodigoBarras.Proximo(), 3.5m, 5.99m, categoria.Id,
            UnidadeMedida.Quilograma, EstoqueMinimo: 5m, EstoqueInicial: 10m));
        respostaProduto.StatusCode.Should().Be(HttpStatusCode.Created);
        (await respostaProduto.Content.ReadAsStringAsync()).Should().Contain("\"unidadeMedida\":\"Quilograma\"");
        var produto = (await LerAsync<ProdutoDto>(respostaProduto))!;
        respostaProduto.Headers.Location!.AbsolutePath.Should().Be($"/api/produtos/{produto.Id}");
        produto.Estoque.Should().Be(10m);
        produto.AbaixoDoEstoqueMinimo.Should().BeFalse();
        produto.CategoriaId.Should().Be(categoria.Id);

        // 3. Entrada de estoque
        var entrada = await PostAsync($"/api/produtos/{produto.Id}/estoque/entrada", new MovimentarEstoqueRequest(0.5m));
        entrada.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<ProdutoDto>(entrada))!.Estoque.Should().Be(10.5m);

        // 4. Saída de estoque: 10,5 - 5,625 = 4,875 (abaixo do mínimo de 5)
        var saida = await PostAsync($"/api/produtos/{produto.Id}/estoque/saida", new MovimentarEstoqueRequest(5.625m));
        saida.StatusCode.Should().Be(HttpStatusCode.OK);
        var aposSaida = (await LerAsync<ProdutoDto>(saida))!;
        aposSaida.Estoque.Should().Be(4.875m);
        aposSaida.AbaixoDoEstoqueMinimo.Should().BeTrue();

        // 5. Consultas
        var porId = await Client.GetAsync($"/api/produtos/{produto.Id}");
        porId.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<ProdutoDto>(porId)).Should().Be(aposSaida);

        var lista = (await LerAsync<List<ProdutoDto>>(await Client.GetAsync("/api/produtos")))!;
        lista.Should().ContainSingle().Which.Should().Be(aposSaida);

        var emFalta = (await LerAsync<List<ProdutoDto>>(await Client.GetAsync("/api/produtos/abaixo-estoque-minimo")))!;
        emFalta.Should().ContainSingle().Which.Id.Should().Be(produto.Id);

        var categorias = (await LerAsync<List<CategoriaDto>>(await Client.GetAsync("/api/categorias")))!;
        categorias.Should().ContainSingle().Which.Should().Be(categoria);

        var categoriaPorId = await Client.GetAsync($"/api/categorias/{categoria.Id}");
        categoriaPorId.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<CategoriaDto>(categoriaPorId)).Should().Be(categoria);
    }

    [Fact]
    public async Task Atualizar_preco_deve_responder_200_com_os_novos_precos()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id);

        var resposta = await PutAsync($"/api/produtos/{produto.Id}/preco", new AtualizarPrecoRequest(6m, 9.5m));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = (await LerAsync<ProdutoDto>(resposta))!;
        atualizado.PrecoCusto.Should().Be(6m);
        atualizado.PrecoVenda.Should().Be(9.5m);
    }

    [Fact]
    public async Task Produto_sem_estoque_minimo_atingido_nao_deve_aparecer_no_alerta()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        await CriarProdutoViaApiAsync(categoria.Id, estoqueMinimo: 10m, estoqueInicial: 10m);

        var emFalta = (await LerAsync<List<ProdutoDto>>(await Client.GetAsync("/api/produtos/abaixo-estoque-minimo")))!;

        emFalta.Should().BeEmpty();
    }
}
