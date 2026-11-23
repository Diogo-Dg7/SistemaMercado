using System.Globalization;
using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.ValueObjects;

/// <summary>
/// Valor monetário imutável, não negativo, com 2 casas decimais.
/// </summary>
public sealed record Dinheiro : IComparable<Dinheiro>
{
    public static Dinheiro Zero { get; } = new(0m);

    public Dinheiro(decimal valor)
    {
        if (valor < 0)
            throw new DomainException("O valor monetário não pode ser negativo.");

        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    // Somente get: impede que `with { Valor = ... }` burle a validação.
    public decimal Valor { get; }

    public Dinheiro Multiplicar(int quantidade)
    {
        if (quantidade < 0)
            throw new DomainException("A quantidade não pode ser negativa.");

        return new Dinheiro(Valor * quantidade);
    }

    public int CompareTo(Dinheiro? other) => other is null ? 1 : Valor.CompareTo(other.Valor);

    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => new(a.Valor + b.Valor);

    public static Dinheiro operator -(Dinheiro a, Dinheiro b) => new(a.Valor - b.Valor);

    public static bool operator <(Dinheiro a, Dinheiro b) => a.CompareTo(b) < 0;

    public static bool operator >(Dinheiro a, Dinheiro b) => a.CompareTo(b) > 0;

    public static bool operator <=(Dinheiro a, Dinheiro b) => a.CompareTo(b) <= 0;

    public static bool operator >=(Dinheiro a, Dinheiro b) => a.CompareTo(b) >= 0;

    public override string ToString() => Valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
}
