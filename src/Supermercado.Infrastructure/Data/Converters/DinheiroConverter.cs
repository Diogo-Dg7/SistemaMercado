using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Infrastructure.Data.Converters;

/// <summary>Dinheiro (value object) &lt;-&gt; decimal no banco.</summary>
public class DinheiroConverter : ValueConverter<Dinheiro, decimal>
{
    public DinheiroConverter()
        : base(dinheiro => dinheiro.Valor, valor => new Dinheiro(valor))
    {
    }
}
