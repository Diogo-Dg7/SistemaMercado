using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.API.IntegrationTests.Endpoints;

[Collection(BancoCollection.Nome)]
public class UsuariosEndpointsTests : ApiTestBase
{
    public UsuariosEndpointsTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    [Fact]
    public async Task Gerente_cria_usuario_e_o_novo_usuario_consegue_fazer_login_com_o_perfil_certo()
    {
        var resposta = await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Beto Souza", "Beto@Mercado.com", "senha-do-beto-1", PerfilUsuario.Operador));

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var criado = (await LerAsync<UsuarioDto>(resposta))!;
        criado.Nome.Should().Be("Beto Souza");
        criado.Email.Should().Be("beto@mercado.com"); // normalizado
        criado.Perfil.Should().Be(PerfilUsuario.Operador);
        criado.Ativo.Should().BeTrue();

        var login = await ClienteAnonimo().PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("beto@mercado.com", "senha-do-beto-1"), JsonTeste.Opcoes);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<LoginResponse>(login))!.Perfil.Should().Be(PerfilUsuario.Operador);
    }

    [Fact]
    public async Task Resposta_nunca_deve_conter_senha_nem_hash()
    {
        var criacao = await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Beto", "beto@mercado.com", "senha-do-beto-1", PerfilUsuario.Operador));
        var listagem = await Client.GetAsync("/api/usuarios");

        foreach (var texto in new[] { await criacao.Content.ReadAsStringAsync(), await listagem.Content.ReadAsStringAsync() })
            texto.ToLowerInvariant().Should().NotContain("senha").And.NotContain("hash").And.NotContain("senha-do-beto-1");
    }

    [Fact]
    public async Task Senha_deve_ser_guardada_apenas_como_hash()
    {
        await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Beto", "beto@mercado.com", "senha-do-beto-1", PerfilUsuario.Operador));

        using var escopo = Banco.NovoEscopo();
        var usuario = (await escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>()
            .ObterPorEmailAsync(new Email("beto@mercado.com")))!;

        usuario.SenhaHash.Should().NotBe("senha-do-beto-1").And.NotContain("senha-do-beto-1");
        usuario.SenhaHash.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public async Task Listar_deve_retornar_os_usuarios_em_ordem_alfabetica()
    {
        await CriarUsuarioNoBancoAsync("zeca@mercado.com", "senha-do-zeca-1", nome: "Zeca");
        await CriarUsuarioNoBancoAsync("ana@mercado.com", "senha-da-ana-123", PerfilUsuario.Gerente, nome: "Ana");

        var usuarios = (await LerAsync<List<UsuarioDto>>(await Client.GetAsync("/api/usuarios")))!;

        usuarios.Select(u => u.Nome).Should().Equal("Ana", "Zeca");
    }

    [Fact]
    public async Task Email_ja_cadastrado_deve_responder_422()
    {
        await CriarUsuarioNoBancoAsync("ana@mercado.com", "senha-da-ana-123");

        var resposta = await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Outra Ana", "ANA@mercado.com", "outra-senha-123", PerfilUsuario.Operador));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("e-mail");
    }

    [Theory]
    [InlineData("curta")]
    [InlineData("1234567")]
    public async Task Senha_com_menos_de_8_caracteres_deve_responder_400(string senha)
    {
        var resposta = await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Beto", "beto@mercado.com", senha, PerfilUsuario.Operador));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await LerProblemaAsync(resposta);
        corpo.GetProperty("errors").TryGetProperty("senha", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Email_invalido_deve_responder_400()
    {
        var resposta = await PostAsync("/api/usuarios",
            new CriarUsuarioRequest("Beto", "nao-e-email", "senha-do-beto-1", PerfilUsuario.Operador));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LerProblemaAsync(resposta)).GetProperty("errors").TryGetProperty("email", out _).Should().BeTrue();
    }
}
