using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Tests.ValueObjects;

public class CodigoBarrasTests
{
    [Theory]
    [InlineData("96385074")]         // EAN-8
    [InlineData("036000291452")]     // UPC-A
    [InlineData("4006381333931")]    // EAN-13
    [InlineData("10012345678902")]   // GTIN-14
    public void Deve_aceitar_codigos_validos(string codigo)
    {
        new CodigoBarras(codigo).Valor.Should().Be(codigo);
    }

    [Fact]
    public void Deve_remover_espacos_nas_pontas()
    {
        new CodigoBarras("  4006381333931 ").Valor.Should().Be("4006381333931");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_vazio(string? codigo)
    {
        var act = () => new CodigoBarras(codigo!);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("4006381A33931")]
    [InlineData("4006-381333931")]
    public void Deve_rejeitar_caracteres_nao_numericos(string codigo)
    {
        var act = () => new CodigoBarras(codigo);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("400638133393")]
    [InlineData("400638133393100")]
    public void Deve_rejeitar_tamanho_invalido(string codigo)
    {
        var act = () => new CodigoBarras(codigo);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("96385075")]
    [InlineData("4006381333932")]
    public void Deve_rejeitar_digito_verificador_invalido(string codigo)
    {
        var act = () => new CodigoBarras(codigo);

        act.Should().Throw<DomainException>().WithMessage("*verificador*");
    }

    [Fact]
    public void TryCriar_deve_retornar_codigo_para_valor_valido()
    {
        CodigoBarras.TryCriar(" 4006381333931 ", out var codigo, out var erro).Should().BeTrue();

        codigo.Should().Be(new CodigoBarras("4006381333931"));
        erro.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "obrigatório")]
    [InlineData("12AB5678", "dígitos")]
    [InlineData("1234567", "8, 12, 13 ou 14")]
    [InlineData("96385075", "verificador")]
    public void TryCriar_deve_retornar_motivo_para_valor_invalido(string? valor, string trechoDoErro)
    {
        CodigoBarras.TryCriar(valor, out var codigo, out var erro).Should().BeFalse();

        codigo.Should().BeNull();
        erro.Should().Contain(trechoDoErro);
    }

    [Fact]
    public void Records_com_mesmo_valor_devem_ser_iguais()
    {
        new CodigoBarras("4006381333931").Should().Be(new CodigoBarras("4006381333931"));
    }
}
