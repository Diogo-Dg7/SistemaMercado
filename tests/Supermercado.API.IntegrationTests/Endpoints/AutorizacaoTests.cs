using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Application.Security;
using Supermercado.Domain.Enums;
using Supermercado.Infrastructure.Services;

namespace Supermercado.API.IntegrationTests.Endpoints;

[Collection(BancoCollection.Nome)]
public class AutorizacaoTests : ApiTestBase
{
    public AutorizacaoTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    private static readonly Guid IdQualquer = Guid.NewGuid();

    private static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string titulo)
    {
        resposta.StatusCode.Should().Be(status);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var corpo = await resposta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        corpo.GetProperty("status").GetInt32().Should().Be((int)status);
        corpo.GetProperty("title").GetString().Should().Be(titulo);
    }

    // ---------- 401: sem token ou token inválido ----------

    public static TheoryData<string, string> EndpointsProtegidos => new()
    {
        { "GET", "/api/categorias" },
        { "GET", "/api/produtos" },
        { "GET", "/api/produtos/abaixo-estoque-minimo" },
        { "POST", "/api/produtos" },
        { "PUT", "/api/produtos/00000000-0000-0000-0000-000000000001/preco" },
        { "POST", "/api/produtos/00000000-0000-0000-0000-000000000001/estoque/saida" },
        { "GET", "/api/usuarios" },
        { "POST", "/api/usuarios" },
        { "GET", "/api/auth/me" },
    };

    [Theory]
    [MemberData(nameof(EndpointsProtegidos))]
    public async Task Endpoint_sem_token_deve_responder_401_em_ProblemDetails(string metodo, string url)
    {
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), url);
        if (metodo != "GET")
            requisicao.Content = JsonContent.Create(new { });

        var resposta = await ClienteAnonimo().SendAsync(requisicao);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.Unauthorized, "Não autorizado");
        resposta.Headers.WwwAuthenticate.Should().Contain(h => h.Scheme == "Bearer");
    }

    [Fact]
    public async Task Rotas_anonimas_devem_continuar_acessiveis()
    {
        (await ClienteAnonimo().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ClienteAnonimo().PostAsJsonAsync("/api/auth/login", new LoginRequest("x@y.com", "senha-qualquer")))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized); // chega ao login (401 de credenciais), não é barrada antes
    }

    [Fact]
    public async Task Token_com_lixo_deve_responder_401()
    {
        var cliente = ClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "isto.nao.e-um-jwt");

        (await cliente.GetAsync("/api/produtos")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public static TheoryData<string> TokensInvalidos => new() { "chave-errada", "expirado", "issuer-errado", "audience-errada" };

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task Token_assinado_com_chave_errada_expirado_ou_de_outro_emissor_deve_responder_401(string defeito)
    {
        var jwt = Api.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var agora = DateTime.UtcNow;

        var chave = defeito == "chave-errada" ? "outra-chave-com-mais-de-32-caracteres-0123456789" : jwt.Chave;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = defeito == "issuer-errado" ? "outro-emissor" : jwt.Issuer,
            Audience = defeito == "audience-errada" ? "outro-publico" : jwt.Audience,
            IssuedAt = defeito == "expirado" ? agora.AddHours(-9) : agora,
            NotBefore = defeito == "expirado" ? agora.AddHours(-9) : agora,
            Expires = defeito == "expirado" ? agora.AddHours(-1) : agora.AddHours(1),
            Subject = new ClaimsIdentity(
            [
                new Claim(NomesDeClaim.Id, IdQualquer.ToString()),
                new Claim(NomesDeClaim.Nome, "Invasor"),
                new Claim(NomesDeClaim.Email, "invasor@teste.com"),
                new Claim(NomesDeClaim.Perfil, nameof(PerfilUsuario.Gerente)),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256),
        });

        var cliente = ClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await cliente.GetAsync("/api/produtos")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_valido_do_mesmo_emissor_deve_ser_aceito()
    {
        // Contraprova dos casos acima: o mesmo procedimento, sem defeito, autentica.
        var cliente = ClienteComo(PerfilUsuario.Operador);

        (await cliente.GetAsync("/api/produtos")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- 403: Operador em endpoint de Gerente ----------

    public static TheoryData<string, string> EndpointsSoDeGerente => new()
    {
        { "POST", "/api/categorias" },
        { "POST", "/api/produtos" },
        { "POST", "/api/produtos/00000000-0000-0000-0000-000000000001/estoque/entrada" },
        { "GET", "/api/usuarios" },
        { "POST", "/api/usuarios" },
    };

    [Theory]
    [MemberData(nameof(EndpointsSoDeGerente))]
    public async Task Operador_em_endpoint_de_Gerente_deve_responder_403_em_ProblemDetails(string metodo, string url)
    {
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), url);
        if (metodo != "GET")
            requisicao.Content = JsonContent.Create(new { });

        var resposta = await ClienteComo(PerfilUsuario.Operador).SendAsync(requisicao);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.Forbidden, "Acesso negado");
    }

    [Fact]
    public async Task Operador_nao_deve_conseguir_criar_nada_no_banco()
    {
        var operador = ClienteComo(PerfilUsuario.Operador);

        await operador.PostAsJsonAsync("/api/categorias", new CriarCategoriaRequest("Invasão"), JsonTeste.Opcoes);

        (await LerAsync<List<CategoriaDto>>(await Client.GetAsync("/api/categorias")))!.Should().BeEmpty();
    }

    [Fact]
    public async Task Gerente_deve_acessar_os_endpoints_de_Gerente()
    {
        var gerente = ClienteComo(PerfilUsuario.Gerente);

        (await gerente.GetAsync("/api/usuarios")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await gerente.PostAsJsonAsync("/api/categorias", new CriarCategoriaRequest("Bebidas"), JsonTeste.Opcoes))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---------- Operador: o que pode ----------

    [Fact]
    public async Task Operador_deve_conseguir_consultar_dar_saida_de_estoque_e_alterar_preco()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id, custo: 5m, venda: 8m, estoqueInicial: 20m);
        var operador = ClienteComo(PerfilUsuario.Operador);

        (await operador.GetAsync("/api/categorias")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await operador.GetAsync($"/api/categorias/{categoria.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await operador.GetAsync("/api/produtos")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await operador.GetAsync($"/api/produtos/{produto.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await operador.GetAsync("/api/produtos/abaixo-estoque-minimo")).StatusCode.Should().Be(HttpStatusCode.OK);

        var saida = await operador.PostAsJsonAsync(
            $"/api/produtos/{produto.Id}/estoque/saida", new MovimentarEstoqueRequest(5m), JsonTeste.Opcoes);
        saida.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<ProdutoDto>(saida))!.Estoque.Should().Be(15m);

        var preco = await operador.PutAsJsonAsync(
            $"/api/produtos/{produto.Id}/preco", new AtualizarPrecoRequest(6m, 9m), JsonTeste.Opcoes);
        preco.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- Preço abaixo do custo: depende do perfil ----------

    [Fact]
    public async Task Operador_com_preco_abaixo_do_custo_deve_receber_422_e_Gerente_deve_receber_200()
    {
        var categoria = await CriarCategoriaViaApiAsync();
        var produto = await CriarProdutoViaApiAsync(categoria.Id, custo: 5m, venda: 8m);
        var url = $"/api/produtos/{produto.Id}/preco";
        var abaixoDoCusto = new AtualizarPrecoRequest(PrecoCusto: 10m, PrecoVenda: 9m);

        var operador = await ClienteComo(PerfilUsuario.Operador).PutAsJsonAsync(url, abaixoDoCusto, JsonTeste.Opcoes);
        operador.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await operador.Content.ReadAsStringAsync()).Should().Contain("menor que o preço de custo");
        (await LerAsync<ProdutoDto>(await Client.GetAsync($"/api/produtos/{produto.Id}")))!
            .PrecoVenda.Should().Be(8m, "a tentativa do Operador não pode ter alterado o preço");

        var gerente = await ClienteComo(PerfilUsuario.Gerente).PutAsJsonAsync(url, abaixoDoCusto, JsonTeste.Opcoes);
        gerente.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = (await LerAsync<ProdutoDto>(gerente))!;
        atualizado.PrecoCusto.Should().Be(10m);
        atualizado.PrecoVenda.Should().Be(9m);
    }
}
