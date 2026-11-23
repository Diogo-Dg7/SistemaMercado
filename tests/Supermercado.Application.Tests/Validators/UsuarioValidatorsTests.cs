using FluentValidation.TestHelper;
using Supermercado.Application.DTOs;
using Supermercado.Application.Validators;
using Supermercado.Domain.Enums;

namespace Supermercado.Application.Tests.Validators;

public class UsuarioValidatorsTests
{
    private static CriarUsuarioRequest UsuarioValido() =>
        new("Ana Silva", "ana@mercado.com", "senha-segura-1", PerfilUsuario.Operador);

    // ---------- CriarUsuarioRequest ----------

    [Fact]
    public void CriarUsuario_valido_nao_deve_ter_erros()
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CriarUsuario_com_senha_de_exatamente_8_caracteres_deve_ser_aceita()
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido() with { Senha = "12345678" })
            .ShouldNotHaveValidationErrorFor(x => x.Senha);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void CriarUsuario_com_senha_menor_que_8_caracteres_deve_falhar(string senha)
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido() with { Senha = senha })
            .ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Fact]
    public void CriarUsuario_com_senha_maior_que_128_caracteres_deve_falhar()
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido() with { Senha = new string('a', 129) })
            .ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    public void CriarUsuario_com_email_invalido_deve_falhar(string email)
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido() with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void CriarUsuario_com_nome_vazio_ou_longo_deve_falhar()
    {
        var validador = new CriarUsuarioRequestValidator();

        validador.TestValidate(UsuarioValido() with { Nome = "" }).ShouldHaveValidationErrorFor(x => x.Nome);
        validador.TestValidate(UsuarioValido() with { Nome = new string('a', 101) }).ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void CriarUsuario_com_perfil_invalido_deve_falhar()
    {
        new CriarUsuarioRequestValidator().TestValidate(UsuarioValido() with { Perfil = (PerfilUsuario)99 })
            .ShouldHaveValidationErrorFor(x => x.Perfil);
    }

    // ---------- LoginRequest ----------

    [Fact]
    public void Login_valido_nao_deve_ter_erros()
    {
        new LoginRequestValidator().TestValidate(new LoginRequest("ana@mercado.com", "qualquer"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Login_com_email_invalido_ou_senha_vazia_deve_falhar()
    {
        var validador = new LoginRequestValidator();

        validador.TestValidate(new LoginRequest("nao-e-email", "x")).ShouldHaveValidationErrorFor(x => x.Email);
        validador.TestValidate(new LoginRequest("ana@mercado.com", "")).ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Fact]
    public void Login_nao_deve_aplicar_o_minimo_de_8_caracteres_da_senha()
    {
        new LoginRequestValidator().TestValidate(new LoginRequest("ana@mercado.com", "abc"))
            .ShouldNotHaveValidationErrorFor(x => x.Senha);
    }
}
