using System.Net;
using Supermercado.API.IntegrationTests.Infrastructure;

namespace Supermercado.API.IntegrationTests.Endpoints;

/// <summary>OpenAPI, Scalar e CORS existem só em Development.</summary>
[Collection(BancoCollection.Nome)]
public class AmbienteDevelopmentTests : ApiTestBase
{
    private const string OrigemVite = "http://localhost:5173";

    public AmbienteDevelopmentTests(BancoDeDadosFixture banco) : base(banco, "Development")
    {
    }

    private static HttpRequestMessage Requisicao(HttpMethod metodo, string url, string origem)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Add("Origin", origem);
        return requisicao;
    }

    [Fact]
    public async Task OpenApi_deve_descrever_os_endpoints()
    {
        var resposta = await Client.GetAsync("/openapi/v1.json");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var documento = await resposta.Content.ReadAsStringAsync();
        documento.Should().Contain("/api/produtos")
            .And.Contain("/api/produtos/{id}/preco")
            .And.Contain("/api/produtos/{id}/estoque/entrada")
            .And.Contain("/api/produtos/{id}/estoque/saida")
            .And.Contain("/api/produtos/abaixo-estoque-minimo")
            .And.Contain("/api/categorias");
    }

    [Fact]
    public async Task OpenApi_deve_descrever_enums_como_texto()
    {
        var documento = await (await Client.GetAsync("/openapi/v1.json")).Content.ReadAsStringAsync();

        documento.Should().Contain("Quilograma");
    }

    [Fact]
    public async Task Scalar_deve_estar_disponivel_em_scalar()
    {
        var resposta = await Client.GetAsync("/scalar");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Cors_deve_liberar_o_vite_na_preflight()
    {
        var requisicao = Requisicao(HttpMethod.Options, "/api/produtos", OrigemVite);
        requisicao.Headers.Add("Access-Control-Request-Method", "POST");
        requisicao.Headers.Add("Access-Control-Request-Headers", "content-type");

        var resposta = await Client.SendAsync(requisicao);

        resposta.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(OrigemVite);
    }

    [Fact]
    public async Task Cors_nao_deve_liberar_outras_origens()
    {
        var resposta = await Client.SendAsync(Requisicao(HttpMethod.Get, "/api/produtos", "http://site-qualquer.com"));

        resposta.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Cors_deve_estar_presente_tambem_nas_respostas_de_erro()
    {
        // Sem isso o navegador esconderia o ProblemDetails (404/409/422) atrás de um erro de CORS.
        var resposta = await Client.SendAsync(Requisicao(HttpMethod.Get, $"/api/produtos/{Guid.NewGuid()}", OrigemVite));

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resposta.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(OrigemVite);
    }
}

[Collection(BancoCollection.Nome)]
public class AmbienteProductionTests : ApiTestBase
{
    public AmbienteProductionTests(BancoDeDadosFixture banco) : base(banco, "Production")
    {
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Documentacao_nao_deve_ser_exposta_fora_de_Development(string url)
    {
        var resposta = await Client.GetAsync(url);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cors_nao_deve_ser_liberado_fora_de_Development()
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/produtos");
        requisicao.Headers.Add("Origin", "http://localhost:5173");

        var resposta = await Client.SendAsync(requisicao);

        resposta.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Api_deve_funcionar_normalmente_fora_de_Development()
    {
        (await Client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.GetAsync("/api/produtos")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
