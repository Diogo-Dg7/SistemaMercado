using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Application.Security;
using Supermercado.Domain.Enums;

namespace Supermercado.API.IntegrationTests.Endpoints;

[Collection(BancoCollection.Nome)]
public class LoginTests : ApiTestBase
{
    private const string Email = "ana@mercado.com";
    private const string Senha = "senha-da-ana-123";

    public LoginTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string senha) =>
        ClienteAnonimo().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, senha), JsonTeste.Opcoes);

    /// <summary>Corpo do ProblemDetails sem o traceId (único campo que muda entre requisições).</summary>
    private static async Task<string> CorpoSemTraceIdAsync(HttpResponseMessage resposta)
    {
        var corpo = await resposta.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        corpo!.Remove("traceId");
        return JsonSerializer.Serialize(
            corpo.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value),
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    // ---------- Login válido ----------

    [Fact]
    public async Task Login_valido_deve_retornar_token_expiracao_nome_e_perfil()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha, PerfilUsuario.Gerente, nome: "Ana Silva");

        var resposta = await LoginAsync(Email, Senha);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await LerAsync<LoginResponse>(resposta))!;
        login.Token.Should().NotBeNullOrWhiteSpace();
        login.Nome.Should().Be("Ana Silva");
        login.Perfil.Should().Be(PerfilUsuario.Gerente);
        login.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("\"perfil\":\"Gerente\"");
    }

    [Fact]
    public async Task Token_do_login_deve_ter_as_claims_de_id_nome_email_e_perfil_e_validade_de_8_horas()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha, PerfilUsuario.Operador, nome: "Ana Silva");

        var login = (await LerAsync<LoginResponse>(await LoginAsync(Email, Senha)))!;

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(login.Token);
        Guid.TryParse(jwt.GetClaim(NomesDeClaim.Id).Value, out var id).Should().BeTrue();
        id.Should().NotBeEmpty();
        jwt.GetClaim(NomesDeClaim.Nome).Value.Should().Be("Ana Silva");
        jwt.GetClaim(NomesDeClaim.Email).Value.Should().Be(Email);
        jwt.GetClaim(NomesDeClaim.Perfil).Value.Should().Be("Operador");
        (jwt.ValidTo - jwt.ValidFrom).Should().BeCloseTo(TimeSpan.FromHours(8), TimeSpan.FromSeconds(5));
        jwt.Alg.Should().Be("HS256");
    }

    [Fact]
    public async Task Token_do_login_deve_funcionar_no_endpoint_me()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha, PerfilUsuario.Operador, nome: "Ana Silva");
        var login = (await LerAsync<LoginResponse>(await LoginAsync(Email, Senha)))!;

        var cliente = ClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var resposta = await cliente.GetAsync("/api/auth/me");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var eu = (await LerAsync<UsuarioLogadoDto>(resposta))!;
        eu.Nome.Should().Be("Ana Silva");
        eu.Email.Should().Be(Email);
        eu.Perfil.Should().Be(PerfilUsuario.Operador);
    }

    [Fact]
    public async Task Login_deve_ignorar_caixa_e_espacos_no_email()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha);

        var resposta = await LoginAsync("  ANA@Mercado.COM ", Senha);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- Falhas: sempre a mesma resposta 401 ----------

    [Fact]
    public async Task Senha_errada_email_inexistente_e_usuario_inativo_devem_receber_exatamente_a_mesma_resposta_401()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha);
        await CriarUsuarioNoBancoAsync("inativo@mercado.com", Senha, ativo: false);

        var senhaErrada = await LoginAsync(Email, "senha-errada-123");
        var emailInexistente = await LoginAsync("ninguem@mercado.com", Senha);
        var usuarioInativo = await LoginAsync("inativo@mercado.com", Senha); // senha CORRETA, mas inativo

        foreach (var resposta in new[] { senhaErrada, emailInexistente, usuarioInativo })
        {
            resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        var corpos = new[]
        {
            await CorpoSemTraceIdAsync(senhaErrada),
            await CorpoSemTraceIdAsync(emailInexistente),
            await CorpoSemTraceIdAsync(usuarioInativo),
        };
        corpos.Distinct().Should().ContainSingle("os três motivos de falha não podem ser distinguíveis");
        corpos[0].Should().Contain("E-mail ou senha inválidos");
    }

    [Fact]
    public async Task Falha_de_login_nao_deve_vazar_detalhes()
    {
        await CriarUsuarioNoBancoAsync(Email, Senha);

        var texto = await (await LoginAsync(Email, "senha-errada-123")).Content.ReadAsStringAsync();

        texto.Should().NotContain(Email).And.NotContain("Exception").And.NotContain("   at ");
    }

    // ---------- Validação (400) ----------

    [Theory]
    [InlineData("", "qualquer")]
    [InlineData("nao-e-email", "qualquer")]
    [InlineData("ana@mercado.com", "")]
    public async Task Login_com_dados_invalidos_deve_responder_400(string email, string senha)
    {
        var resposta = await LoginAsync(email, senha);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
