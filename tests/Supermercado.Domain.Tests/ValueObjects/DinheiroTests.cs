using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Tests.ValueObjects;

public class DinheiroTests
{
    [Fact]
    public void Deve_criar_com_valor_valido()
    {
        new Dinheiro(10.50m).Valor.Should().Be(10.50m);
    }

    [Fact]
    public void Deve_aceitar_zero()
    {
        new Dinheiro(0m).Should().Be(Dinheiro.Zero);
    }

    [Fact]
    public void Deve_rejeitar_valor_negativo()
    {
        var act = () => new Dinheiro(-0.01m);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(1.004, 1.00)]
    [InlineData(2.999, 3.00)]
    public void Deve_arredondar_para_duas_casas(double entrada, double esperado)
    {
        new Dinheiro((decimal)entrada).Valor.Should().Be((decimal)esperado);
    }

    [Fact]
    public void Records_com_mesmo_valor_devem_ser_iguais()
    {
        new Dinheiro(5m).Should().Be(new Dinheiro(5.00m));
        (new Dinheiro(5m) == new Dinheiro(5m)).Should().BeTrue();
    }

    [Fact]
    public void Soma_deve_somar_valores()
    {
        (new Dinheiro(1.10m) + new Dinheiro(2.20m)).Should().Be(new Dinheiro(3.30m));
    }

    [Fact]
    public void Subtracao_deve_subtrair_valores()
    {
        (new Dinheiro(5m) - new Dinheiro(2m)).Should().Be(new Dinheiro(3m));
    }

    [Fact]
    public void Subtracao_com_resultado_negativo_deve_lancar_excecao()
    {
        var act = () => new Dinheiro(1m) - new Dinheiro(2m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Multiplicar_deve_multiplicar_pela_quantidade()
    {
        new Dinheiro(2.50m).Multiplicar(3).Should().Be(new Dinheiro(7.50m));
    }

    [Fact]
    public void Multiplicar_por_quantidade_negativa_deve_lancar_excecao()
    {
        var act = () => new Dinheiro(2.50m).Multiplicar(-1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Operadores_de_comparacao_devem_funcionar()
    {
        var menor = new Dinheiro(1m);
        var maior = new Dinheiro(2m);

        (menor < maior).Should().BeTrue();
        (maior > menor).Should().BeTrue();
        (menor <= new Dinheiro(1m)).Should().BeTrue();
        (maior >= new Dinheiro(2m)).Should().BeTrue();
        (maior < menor).Should().BeFalse();
    }
}
