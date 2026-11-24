using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Infrastructure.Data.Converters;

/// <summary>Email (value object) &lt;-&gt; string no banco.</summary>
public class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(email => email.Valor, valor => new Email(valor))
    {
    }
}
