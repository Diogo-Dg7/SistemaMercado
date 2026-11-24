using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Enums;

namespace Supermercado.API.IntegrationTests.Endpoints;

/// <summary>Cada código de erro da API, de ponta a ponta, sempre como ProblemDetails.</summary>
[Collection(BancoCollection.Nome)]
public class ErrosHttpTests : ApiTestBase
{
    public ErrosHttpTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    private static void DeveSerProblemDetails(HttpResponseMessage resposta, HttpStatusCode status, JsonElement corpo)
    {
        resposta.StatusCode.Should().Be(status);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        corpo.GetProperty("status").GetInt32().Should().Be((int)status);
        corpo.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ---------- 400 ----------

    [Fact]
    public async Task Categoria_com_nome_vazio_deve_responder_400_com_erros_por_campo()
    {
        var resposta = await PostAsync("/api/categorias", new CriarCategoriaRequest(""));

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.BadRequest, problema);
        problema.GetProperty("errors").GetProperty("nome").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Produto_invalido_deve_responder_400_listando_todos_os_campos_com_erro()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var request = new CriarProdutoRequest("", "123", -1m, 5m, categoria.Id, UnidadeMedida.Unidade, -1m, -1m);

        var resposta = await PostAsync("/api/produtos", request);

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.BadRequest, problema);
        var campos = problema.GetProperty("errors").EnumerateObject().Select(p => p.Name);
        campos.Should().Contain(["nome", "codigoBarras", "precoCusto", "estoqueMinimo", "estoqueInicial"]);
    }

    [Fact]
    public async Task Quantidade_de_estoque_nao_positiva_deve_responder_400()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id);

        var resposta = await PostAsync($"/api/produtos/{produto.Id}/estoque/saida", new MovimentarEstoqueRequest(0m));

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.BadRequest, problema);
        problema.GetProperty("errors").TryGetProperty("quantidade", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Json_malformado_deve_responder_400_em_ProblemDetails()
    {
        using var conteudo = new StringContent("{ nome: ", Encoding.UTF8, "application/json");

        var resposta = await Client.PostAsync("/api/categorias", conteudo);

        DeveSerProblemDetails(resposta, HttpStatusCode.BadRequest, await LerProblemaAsync(resposta));
    }

    [Fact]
    public async Task Unidade_de_medida_desconhecida_deve_responder_400()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var corpo = new
        {
            nome = "Suco", codigoBarras = GeradorCodigoBarras.Proximo(), precoCusto = 1, precoVenda = 2,
            categoriaId = categoria.Id, unidadeMedida = "Galao"
        };

        var resposta = await Client.PostAsJsonAsync("/api/produtos", corpo);

        DeveSerProblemDetails(resposta, HttpStatusCode.BadRequest, await LerProblemaAsync(resposta));
    }

    // ---------- 404 ----------

    [Fact]
    public async Task Produto_inexistente_deve_responder_404()
    {
        var resposta = await Client.GetAsync($"/api/produtos/{Guid.NewGuid()}");

        DeveSerProblemDetails(resposta, HttpStatusCode.NotFound, await LerProblemaAsync(resposta));
    }

    [Fact]
    public async Task Categoria_inexistente_deve_responder_404()
    {
        var resposta = await Client.GetAsync($"/api/categorias/{Guid.NewGuid()}");

        DeveSerProblemDetails(resposta, HttpStatusCode.NotFound, await LerProblemaAsync(resposta));
    }

    [Fact]
    public async Task Criar_produto_em_categoria_inexistente_deve_responder_404()
    {
        var request = new CriarProdutoRequest(
            "Arroz", GeradorCodigoBarras.Proximo(), 3m, 5m, Guid.NewGuid(), UnidadeMedida.Unidade);

        var resposta = await PostAsync("/api/produtos", request);

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.NotFound, problema);
        problema.GetProperty("detail").GetString().Should().Contain("Categoria");
    }

    [Fact]
    public async Task Movimentar_estoque_de_produto_inexistente_deve_responder_404()
    {
        var resposta = await PostAsync($"/api/produtos/{Guid.NewGuid()}/estoque/entrada", new MovimentarEstoqueRequest(1m));

        DeveSerProblemDetails(resposta, HttpStatusCode.NotFound, await LerProblemaAsync(resposta));
    }

    [Fact]
    public async Task Rota_inexistente_deve_responder_404_em_ProblemDetails()
    {
        var resposta = await Client.GetAsync("/api/nao-existe");

        DeveSerProblemDetails(resposta, HttpStatusCode.NotFound, await LerProblemaAsync(resposta));
    }

    // ---------- 409 ----------

    [Fact]
    public async Task Saida_acima_do_saldo_deve_responder_409()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id, estoqueInicial: 3m);

        var resposta = await PostAsync($"/api/produtos/{produto.Id}/estoque/saida", new MovimentarEstoqueRequest(4m));

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.Conflict, problema);
        problema.GetProperty("detail").GetString().Should().Contain("Estoque insuficiente");

        // O saldo não foi alterado.
        (await LerAsync<ProdutoDto>(await Client.GetAsync($"/api/produtos/{produto.Id}")))!.Estoque.Should().Be(3m);
    }

    // ---------- 422 ----------

    [Fact]
    public async Task Preco_de_venda_abaixo_do_custo_deve_responder_422_para_o_perfil_operador()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id, custo: 5m, venda: 8m);

        var resposta = await ClienteComo(PerfilUsuario.Operador).PutAsJsonAsync(
            $"/api/produtos/{produto.Id}/preco", new AtualizarPrecoRequest(10m, 9m), JsonTeste.Opcoes);

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.UnprocessableEntity, problema);
        problema.GetProperty("detail").GetString().Should().Contain("menor que o preço de custo");
    }

    [Fact]
    public async Task Codigo_de_barras_duplicado_deve_responder_422()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var codigo = GeradorCodigoBarras.Proximo();
        await CriarProdutoViaApiAsync(categoria.Id, codigo);

        var resposta = await PostAsync("/api/produtos", new CriarProdutoRequest(
            "Outro", codigo, 3m, 5m, categoria.Id, UnidadeMedida.Unidade));

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.UnprocessableEntity, problema);
        problema.GetProperty("detail").GetString().Should().Contain("código de barras");
    }

    [Fact]
    public async Task Quantidade_fracionada_em_produto_por_unidade_deve_responder_422()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id, unidade: UnidadeMedida.Unidade);

        var resposta = await PostAsync($"/api/produtos/{produto.Id}/estoque/entrada", new MovimentarEstoqueRequest(1.5m));

        var problema = await LerProblemaAsync(resposta);
        DeveSerProblemDetails(resposta, HttpStatusCode.UnprocessableEntity, problema);
        problema.GetProperty("detail").GetString().Should().Contain("inteiras");
    }

    // ---------- Nenhum detalhe interno vaza ----------

    [Fact]
    public async Task Respostas_de_erro_nao_devem_conter_rastreamento_de_pilha()
    {
        var resposta = await Client.GetAsync($"/api/produtos/{Guid.NewGuid()}");

        var texto = await resposta.Content.ReadAsStringAsync();
        texto.Should().NotContain("   at ").And.NotContain("Exception");
    }
}
