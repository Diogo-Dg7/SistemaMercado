using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Enums;

namespace Supermercado.API.IntegrationTests.Endpoints;

/// <summary>Base dos testes HTTP: sobe a API real (WebApplicationFactory) sobre o banco de testes.</summary>
public abstract class ApiTestBase : IntegracaoTestBase
{
    private readonly ApiFactory _api;

    protected ApiTestBase(BancoDeDadosFixture banco, string ambiente = "Development") : base(banco)
    {
        _api = new ApiFactory(banco.ConnectionString, ambiente);
        Client = _api.CreateClient();
    }

    protected HttpClient Client { get; }

    public override Task DisposeAsync()
    {
        Client.Dispose();
        _api.Dispose();
        return Task.CompletedTask;
    }

    protected Task<HttpResponseMessage> PostAsync<T>(string url, T corpo) =>
        Client.PostAsJsonAsync(url, corpo, JsonTeste.Opcoes);

    protected Task<HttpResponseMessage> PutAsync<T>(string url, T corpo) =>
        Client.PutAsJsonAsync(url, corpo, JsonTeste.Opcoes);

    protected static Task<T?> LerAsync<T>(HttpResponseMessage resposta) =>
        resposta.Content.ReadFromJsonAsync<T>(JsonTeste.Opcoes);

    protected static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>();

    protected async Task<CategoriaDto> CriarCategoriaViaApiAsync(string nome = "Hortifruti")
    {
        var resposta = await PostAsync("/api/categorias", new CriarCategoriaRequest(nome));
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await LerAsync<CategoriaDto>(resposta))!;
    }

    protected async Task<ProdutoDto> CriarProdutoViaApiAsync(
        Guid categoriaId,
        string? codigoBarras = null,
        UnidadeMedida unidade = UnidadeMedida.Unidade,
        decimal custo = 5m,
        decimal venda = 8m,
        decimal estoqueMinimo = 10m,
        decimal estoqueInicial = 20m)
    {
        var request = new CriarProdutoRequest(
            "Produto de teste", codigoBarras ?? GeradorCodigoBarras.Proximo(), custo, venda,
            categoriaId, unidade, estoqueMinimo, estoqueInicial);

        var resposta = await PostAsync("/api/produtos", request);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await LerAsync<ProdutoDto>(resposta))!;
    }
}
