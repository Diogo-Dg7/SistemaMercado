using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.Tests.Exceptions;

public class ExceptionsTests
{
    [Fact]
    public void DomainException_deve_expor_a_mensagem()
    {
        new DomainException("regra violada").Message.Should().Be("regra violada");
    }

    [Fact]
    public void DomainException_deve_preservar_excecao_interna()
    {
        var interna = new InvalidOperationException("causa");

        new DomainException("regra violada", interna).InnerException.Should().BeSameAs(interna);
    }

    [Fact]
    public void EstoqueInsuficienteException_deve_ser_uma_DomainException()
    {
        new EstoqueInsuficienteException(1, 2).Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void EstoqueInsuficienteException_deve_expor_quantidades_e_mensagem()
    {
        var ex = new EstoqueInsuficienteException(quantidadeDisponivel: 3, quantidadeSolicitada: 5);

        ex.QuantidadeDisponivel.Should().Be(3);
        ex.QuantidadeSolicitada.Should().Be(5);
        ex.Message.Should().Contain("3").And.Contain("5");
    }
}
