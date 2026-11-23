using Supermercado.Domain.Entities;
using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.Tests.Entities;

public class CategoriaTests
{
    [Fact]
    public void Deve_criar_com_nome_valido()
    {
        var categoria = new Categoria("Bebidas");

        categoria.Nome.Should().Be("Bebidas");
        categoria.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Deve_remover_espacos_do_nome()
    {
        new Categoria("  Bebidas ").Nome.Should().Be("Bebidas");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Deve_rejeitar_nome_vazio(string? nome)
    {
        var act = () => new Categoria(nome!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_nome_maior_que_o_limite()
    {
        var act = () => new Categoria(new string('a', Categoria.NomeTamanhoMaximo + 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_aceitar_nome_no_limite()
    {
        new Categoria(new string('a', Categoria.NomeTamanhoMaximo)).Nome.Should().HaveLength(Categoria.NomeTamanhoMaximo);
    }

    [Fact]
    public void Renomear_deve_alterar_o_nome()
    {
        var categoria = new Categoria("Bebidas");

        categoria.Renomear("Laticínios");

        categoria.Nome.Should().Be("Laticínios");
    }

    [Fact]
    public void Renomear_com_nome_invalido_deve_lancar_excecao_e_manter_o_nome()
    {
        var categoria = new Categoria("Bebidas");

        var act = () => categoria.Renomear(" ");

        act.Should().Throw<DomainException>();
        categoria.Nome.Should().Be("Bebidas");
    }
}
