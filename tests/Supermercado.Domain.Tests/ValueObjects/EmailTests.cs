using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("ana@mercado.com")]
    [InlineData("ana.silva+compras@mercado.com.br")]
    [InlineData("a@b.co")]
    [InlineData("admin@supermercado.local")]
    public void Deve_aceitar_emails_validos(string valor)
    {
        new Email(valor).Valor.Should().Be(valor);
    }

    [Fact]
    public void Deve_normalizar_para_minusculas_e_remover_espacos_das_pontas()
    {
        new Email("  Ana.Silva@Mercado.COM ").Valor.Should().Be("ana.silva@mercado.com");
    }

    [Fact]
    public void Emails_que_diferem_so_na_caixa_devem_ser_iguais()
    {
        new Email("ANA@mercado.com").Should().Be(new Email("ana@MERCADO.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_vazio(string? valor)
    {
        var act = () => new Email(valor!);

        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Theory]
    [InlineData("semarroba.com")]
    [InlineData("@mercado.com")]
    [InlineData("ana@")]
    [InlineData("ana@@mercado.com")]
    [InlineData("ana@mercado")]
    [InlineData("ana@.com")]
    [InlineData("ana@mercado.")]
    [InlineData("ana@merc..ado.com")]
    [InlineData("ana silva@mercado.com")]
    [InlineData("ana@mer cado.com")]
    public void Deve_rejeitar_formato_invalido(string valor)
    {
        var act = () => new Email(valor);

        act.Should().Throw<DomainException>().WithMessage("*inválido*");
    }

    [Fact]
    public void Deve_rejeitar_email_maior_que_o_limite()
    {
        var longo = new string('a', 60) + "@" + new string('b', 200) + ".com";

        var act = () => new Email(longo);

        act.Should().Throw<DomainException>().WithMessage("*no máximo*");
    }

    [Fact]
    public void Deve_rejeitar_parte_local_maior_que_64_caracteres()
    {
        var act = () => new Email(new string('a', 65) + "@mercado.com");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TryCriar_deve_retornar_o_email_normalizado_para_valor_valido()
    {
        Email.TryCriar(" Ana@Mercado.com ", out var email, out var erro).Should().BeTrue();

        email.Should().Be(new Email("ana@mercado.com"));
        erro.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "obrigatório")]
    [InlineData("semarroba", "inválido")]
    public void TryCriar_deve_retornar_o_motivo_para_valor_invalido(string? valor, string trechoDoErro)
    {
        Email.TryCriar(valor, out var email, out var erro).Should().BeFalse();

        email.Should().BeNull();
        erro.Should().Contain(trechoDoErro);
    }
}
