using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Services;
using Supermercado.Application.Validators;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Tests.Services;

public class AuthServiceTests
{
    private const string EmailCadastrado = "ana@mercado.com";
    private const string SenhaCorreta = "senha-correta-123";
    private const string HashDaSenha = "hash-da-senha";

    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly AuthService _service;
    private readonly DateTime _expiraEm = new(2026, 11, 24, 8, 0, 0, DateTimeKind.Utc);

    public AuthServiceTests()
    {
        _hasher.Verificar(HashDaSenha, SenhaCorreta).Returns(true);
        _hasher.Verificar(Arg.Any<string>(), Arg.Is<string>(s => s != SenhaCorreta)).Returns(false);
        _service = new AuthService(_usuarios, _hasher, _tokens, new LoginRequestValidator());
    }

    private Usuario UsuarioCadastrado(PerfilUsuario perfil = PerfilUsuario.Gerente, bool ativo = true)
    {
        var usuario = new Usuario("Ana Silva", new Email(EmailCadastrado), HashDaSenha, perfil);
        if (!ativo)
            usuario.Desativar();
        _usuarios.ObterPorEmailAsync(new Email(EmailCadastrado), Arg.Any<CancellationToken>()).Returns(usuario);
        _tokens.GerarToken(usuario).Returns(new TokenAcesso("jwt-emitido", _expiraEm));
        return usuario;
    }

    // ---------- Caminho feliz ----------

    [Fact]
    public async Task Login_valido_deve_retornar_token_expiracao_nome_e_perfil()
    {
        UsuarioCadastrado(PerfilUsuario.Gerente);

        var resposta = await _service.LoginAsync(new LoginRequest(EmailCadastrado, SenhaCorreta));

        resposta.Should().Be(new LoginResponse("jwt-emitido", _expiraEm, "Ana Silva", PerfilUsuario.Gerente));
    }

    [Fact]
    public async Task Login_deve_ignorar_diferencas_de_caixa_e_espacos_no_email()
    {
        UsuarioCadastrado();

        var resposta = await _service.LoginAsync(new LoginRequest("  ANA@Mercado.COM ", SenhaCorreta));

        resposta.Token.Should().Be("jwt-emitido");
    }

    // ---------- Credenciais inválidas: sempre a mesma resposta ----------

    [Fact]
    public async Task Email_inexistente_deve_lancar_CredenciaisInvalidasException()
    {
        _usuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var act = () => _service.LoginAsync(new LoginRequest("ninguem@mercado.com", SenhaCorreta));

        (await act.Should().ThrowAsync<CredenciaisInvalidasException>())
            .Which.Message.Should().Be("E-mail ou senha inválidos");
        _tokens.DidNotReceiveWithAnyArgs().GerarToken(default!);
    }

    [Fact]
    public async Task Senha_errada_deve_lancar_CredenciaisInvalidasException()
    {
        UsuarioCadastrado();

        var act = () => _service.LoginAsync(new LoginRequest(EmailCadastrado, "senha-errada"));

        (await act.Should().ThrowAsync<CredenciaisInvalidasException>())
            .Which.Message.Should().Be("E-mail ou senha inválidos");
        _tokens.DidNotReceiveWithAnyArgs().GerarToken(default!);
    }

    [Fact]
    public async Task Usuario_inativo_com_senha_correta_deve_lancar_CredenciaisInvalidasException()
    {
        UsuarioCadastrado(ativo: false);

        var act = () => _service.LoginAsync(new LoginRequest(EmailCadastrado, SenhaCorreta));

        (await act.Should().ThrowAsync<CredenciaisInvalidasException>())
            .Which.Message.Should().Be("E-mail ou senha inválidos");
        _tokens.DidNotReceiveWithAnyArgs().GerarToken(default!);
    }

    [Fact]
    public async Task Os_tres_motivos_de_falha_devem_ser_indistinguiveis()
    {
        // Mesmo tipo e mesma mensagem: quem chama não descobre qual dos três casos ocorreu.
        _usuarios.ObterPorEmailAsync(new Email("ninguem@mercado.com"), Arg.Any<CancellationToken>()).Returns((Usuario?)null);
        UsuarioCadastrado(ativo: true);
        var inativo = new Usuario("Inativo", new Email("inativo@mercado.com"), HashDaSenha, PerfilUsuario.Operador);
        inativo.Desativar();
        _usuarios.ObterPorEmailAsync(new Email("inativo@mercado.com"), Arg.Any<CancellationToken>()).Returns(inativo);

        var excecoes = new List<Exception>();
        foreach (var request in new[]
                 {
                     new LoginRequest("ninguem@mercado.com", SenhaCorreta),
                     new LoginRequest(EmailCadastrado, "senha-errada"),
                     new LoginRequest("inativo@mercado.com", SenhaCorreta),
                 })
        {
            try { await _service.LoginAsync(request); }
            catch (Exception ex) { excecoes.Add(ex); }
        }

        excecoes.Should().HaveCount(3);
        excecoes.Select(e => e.GetType()).Distinct().Should().ContainSingle().Which.Should().Be<CredenciaisInvalidasException>();
        excecoes.Select(e => e.Message).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Email_inexistente_deve_gastar_o_mesmo_trabalho_de_hash_de_um_login_real()
    {
        _usuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var act = () => _service.LoginAsync(new LoginRequest("ninguem@mercado.com", SenhaCorreta));

        await act.Should().ThrowAsync<CredenciaisInvalidasException>();
        _hasher.Received(1).Gerar(SenhaCorreta); // evita revelar a existência do e-mail pelo tempo de resposta
    }

    // ---------- Validação ----------

    [Theory]
    [InlineData("", SenhaCorreta)]
    [InlineData("nao-e-email", SenhaCorreta)]
    [InlineData(EmailCadastrado, "")]
    public async Task Request_invalido_deve_lancar_ValidationException_sem_consultar_o_banco(string email, string senha)
    {
        var act = () => _service.LoginAsync(new LoginRequest(email, senha));

        await act.Should().ThrowAsync<ValidationException>();
        await _usuarios.DidNotReceiveWithAnyArgs().ObterPorEmailAsync(default!, default);
    }

    [Fact]
    public async Task Login_nao_deve_exigir_tamanho_minimo_de_senha()
    {
        // A política de 8 caracteres vale para o cadastro; no login uma senha curta é só "senha errada" (401).
        UsuarioCadastrado();

        var act = () => _service.LoginAsync(new LoginRequest(EmailCadastrado, "abc"));

        await act.Should().ThrowAsync<CredenciaisInvalidasException>();
    }
}
