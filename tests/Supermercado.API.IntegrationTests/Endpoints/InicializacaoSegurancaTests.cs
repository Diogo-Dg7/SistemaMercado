using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Interfaces;

namespace Supermercado.API.IntegrationTests.Endpoints;

/// <summary>Primeiro Gerente (seeder) e recusa de subir com configuração de JWT fraca.</summary>
[Collection(BancoCollection.Nome)]
public class InicializacaoSegurancaTests : IntegracaoTestBase
{
    private const string EmailAdmin = "Admin@Mercado.com";
    private const string SenhaAdmin = "senha-do-admin-123";

    public InicializacaoSegurancaTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    private ApiFactory CriarApi(string? chaveJwt, string adminEmail = "", string adminSenha = "") =>
        new(Banco.ConnectionString, chaveJwt, "Development", adminEmail, adminSenha);

    private async Task<List<UsuarioDto>> ListarUsuariosAsync()
    {
        using var escopo = Banco.NovoEscopo();
        return (await escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>().ListarAsync())
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Email.Valor, u.Perfil, u.Ativo)).ToList();
    }

    // ---------- Primeiro Gerente ----------

    [Fact]
    public async Task Sem_usuarios_e_com_AdminInicial_deve_criar_o_primeiro_Gerente_que_consegue_logar()
    {
        using var api = CriarApi(Banco.ChaveJwt, EmailAdmin, SenhaAdmin);
        using var cliente = api.CreateClient(); // inicia a API (e o seeder)

        var usuarios = await ListarUsuariosAsync();
        var admin = usuarios.Should().ContainSingle().Subject;
        admin.Email.Should().Be("admin@mercado.com");
        admin.Perfil.Should().Be(PerfilUsuario.Gerente);
        admin.Ativo.Should().BeTrue();

        var login = await cliente.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("admin@mercado.com", SenhaAdmin), JsonTeste.Opcoes);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<LoginResponse>(JsonTeste.Opcoes))!.Perfil.Should().Be(PerfilUsuario.Gerente);
    }

    [Fact]
    public async Task Senha_do_Gerente_inicial_deve_ser_guardada_apenas_como_hash()
    {
        using var api = CriarApi(Banco.ChaveJwt, EmailAdmin, SenhaAdmin);
        using var cliente = api.CreateClient();

        using var escopo = Banco.NovoEscopo();
        var usuario = (await escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>().ListarAsync()).Single();

        usuario.SenhaHash.Should().NotContain(SenhaAdmin);
    }

    [Fact]
    public async Task Quando_ja_existe_algum_usuario_o_seeder_nao_deve_criar_nada()
    {
        using (var escopo = Banco.NovoEscopo())
        {
            await escopo.ServiceProvider.GetRequiredService<IUsuarioService>().CriarAsync(
                new CriarUsuarioRequest("Operador existente", "op@mercado.com", "senha-do-op-123", PerfilUsuario.Operador));
        }

        using var api = CriarApi(Banco.ChaveJwt, EmailAdmin, SenhaAdmin);
        using var cliente = api.CreateClient();

        var usuarios = await ListarUsuariosAsync();
        usuarios.Should().ContainSingle().Which.Email.Should().Be("op@mercado.com");

        // E o AdminInicial não consegue entrar: não foi criado.
        (await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@mercado.com", SenhaAdmin), JsonTeste.Opcoes))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reiniciar_a_API_nao_deve_duplicar_nem_recriar_o_Gerente_inicial()
    {
        using (var primeira = CriarApi(Banco.ChaveJwt, EmailAdmin, SenhaAdmin))
        using (primeira.CreateClient())
        {
        }

        using var segunda = CriarApi(Banco.ChaveJwt, EmailAdmin, SenhaAdmin);
        using var cliente = segunda.CreateClient();

        (await ListarUsuariosAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Sem_AdminInicial_configurado_a_API_sobe_e_nao_cria_usuario()
    {
        using var api = CriarApi(Banco.ChaveJwt);
        using var cliente = api.CreateClient();

        (await cliente.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListarUsuariosAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AdminInicial_com_senha_fraca_nao_deve_criar_usuario_nem_derrubar_a_API()
    {
        using var api = CriarApi(Banco.ChaveJwt, EmailAdmin, "curta");
        using var cliente = api.CreateClient();

        (await cliente.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListarUsuariosAsync()).Should().BeEmpty();
    }

    // ---------- Recusa de subir sem chave JWT adequada ----------

    [Fact]
    public void API_deve_recusar_subir_sem_a_chave_jwt()
    {
        using var api = CriarApi(chaveJwt: null);

        var act = () => api.CreateClient();

        act.Should().Throw<Exception>().Where(e => e.ToString().Contains("Jwt:Chave não foi configurada"));
    }

    [Theory]
    [InlineData("curta-demais")]
    [InlineData("2345678901234567890123456789012")] // 31 caracteres
    public void API_deve_recusar_subir_com_chave_jwt_de_menos_de_32_caracteres_sem_vazar_a_chave(string chave)
    {
        using var api = CriarApi(chave);

        var act = () => api.CreateClient();

        var excecao = act.Should().Throw<Exception>().Which;
        excecao.ToString().Should().Contain("Jwt:Chave deve ter pelo menos 32 caracteres")
            .And.NotContain(chave, "o valor da chave nunca pode aparecer na mensagem de erro");
    }

    [Fact]
    public void API_deve_aceitar_chave_jwt_com_exatamente_32_caracteres()
    {
        using var api = CriarApi(new string('k', 32));

        var act = () => api.CreateClient().Dispose();

        act.Should().NotThrow();
    }
}
