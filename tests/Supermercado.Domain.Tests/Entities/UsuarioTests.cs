using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Tests.Entities;

public class UsuarioTests
{
    private static readonly Email EmailPadrao = new("ana@mercado.com");

    private static Usuario CriarUsuario(
        string nome = "Ana Silva", string senhaHash = "hash-da-senha", PerfilUsuario perfil = PerfilUsuario.Operador) =>
        new(nome, EmailPadrao, senhaHash, perfil);

    // ---------- Criação ----------

    [Fact]
    public void Deve_criar_usuario_ativo_com_os_dados_informados()
    {
        var usuario = CriarUsuario(perfil: PerfilUsuario.Gerente);

        usuario.Id.Should().NotBeEmpty();
        usuario.Nome.Should().Be("Ana Silva");
        usuario.Email.Should().Be(EmailPadrao);
        usuario.SenhaHash.Should().Be("hash-da-senha");
        usuario.Perfil.Should().Be(PerfilUsuario.Gerente);
        usuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Deve_remover_espacos_do_nome()
    {
        CriarUsuario(nome: "  Ana Silva ").Nome.Should().Be("Ana Silva");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_nome_vazio(string? nome)
    {
        var act = () => CriarUsuario(nome: nome!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_nome_maior_que_o_limite()
    {
        var act = () => CriarUsuario(nome: new string('a', Usuario.NomeTamanhoMaximo + 1));

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Deve_rejeitar_hash_de_senha_vazio(string? senhaHash)
    {
        var act = () => CriarUsuario(senhaHash: senhaHash!);

        act.Should().Throw<DomainException>().WithMessage("*hash*");
    }

    [Fact]
    public void Deve_rejeitar_hash_de_senha_maior_que_o_limite()
    {
        var act = () => CriarUsuario(senhaHash: new string('x', Usuario.SenhaHashTamanhoMaximo + 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_perfil_invalido()
    {
        var act = () => CriarUsuario(perfil: (PerfilUsuario)99);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_email_nulo()
    {
        var act = () => new Usuario("Ana", null!, "hash", PerfilUsuario.Operador);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---------- AlterarPerfil ----------

    [Fact]
    public void AlterarPerfil_deve_trocar_o_perfil()
    {
        var usuario = CriarUsuario(perfil: PerfilUsuario.Operador);

        usuario.AlterarPerfil(PerfilUsuario.Gerente);

        usuario.Perfil.Should().Be(PerfilUsuario.Gerente);
    }

    [Fact]
    public void AlterarPerfil_para_o_mesmo_perfil_nao_deve_fazer_nada()
    {
        var usuario = CriarUsuario(perfil: PerfilUsuario.Gerente);

        var act = () => usuario.AlterarPerfil(PerfilUsuario.Gerente);

        act.Should().NotThrow();
        usuario.Perfil.Should().Be(PerfilUsuario.Gerente);
    }

    [Fact]
    public void AlterarPerfil_com_perfil_invalido_deve_lancar_excecao_e_manter_o_perfil()
    {
        var usuario = CriarUsuario(perfil: PerfilUsuario.Operador);

        var act = () => usuario.AlterarPerfil((PerfilUsuario)0);

        act.Should().Throw<DomainException>();
        usuario.Perfil.Should().Be(PerfilUsuario.Operador);
    }

    // ---------- Desativar / Ativar ----------

    [Fact]
    public void Desativar_deve_inativar_o_usuario()
    {
        var usuario = CriarUsuario();

        usuario.Desativar();

        usuario.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_usuario_ja_inativo_deve_lancar_excecao()
    {
        var usuario = CriarUsuario();
        usuario.Desativar();

        var act = () => usuario.Desativar();

        act.Should().Throw<DomainException>().WithMessage("*já está inativo*");
    }

    [Fact]
    public void Ativar_deve_reativar_o_usuario()
    {
        var usuario = CriarUsuario();
        usuario.Desativar();

        usuario.Ativar();

        usuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Ativar_usuario_ja_ativo_deve_lancar_excecao()
    {
        var usuario = CriarUsuario();

        var act = () => usuario.Ativar();

        act.Should().Throw<DomainException>().WithMessage("*já está ativo*");
    }
}
