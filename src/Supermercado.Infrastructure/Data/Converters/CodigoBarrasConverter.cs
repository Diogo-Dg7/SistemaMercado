using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Infrastructure.Data.Converters;

/// <summary>CodigoBarras (value object) &lt;-&gt; string no banco.</summary>
public class CodigoBarrasConverter : ValueConverter<CodigoBarras, string>
{
    public CodigoBarrasConverter()
        : base(codigo => codigo.Valor, valor => new CodigoBarras(valor))
    {
    }
}
