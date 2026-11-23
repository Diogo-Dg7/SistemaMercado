using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.ValueObjects;

/// <summary>
/// Código de barras GTIN (EAN-8, UPC-A de 12, EAN-13 ou GTIN-14) com dígito verificador válido.
/// </summary>
public sealed record CodigoBarras
{
    private static readonly int[] TamanhosValidos = [8, 12, 13, 14];

    public CodigoBarras(string valor)
    {
        var normalizado = valor?.Trim();

        if (string.IsNullOrEmpty(normalizado))
            throw new DomainException("O código de barras é obrigatório.");

        if (!normalizado.All(char.IsAsciiDigit))
            throw new DomainException("O código de barras deve conter apenas dígitos.");

        if (!TamanhosValidos.Contains(normalizado.Length))
            throw new DomainException("O código de barras deve ter 8, 12, 13 ou 14 dígitos.");

        if (!DigitoVerificadorValido(normalizado))
            throw new DomainException("O dígito verificador do código de barras é inválido.");

        Valor = normalizado;
    }

    public string Valor { get; }

    // Algoritmo GTIN: da direita para a esquerda (sem o verificador), pesos alternados 3 e 1.
    private static bool DigitoVerificadorValido(string codigo)
    {
        var soma = 0;
        for (var i = codigo.Length - 2; i >= 0; i--)
        {
            var digito = codigo[i] - '0';
            var peso = (codigo.Length - 2 - i) % 2 == 0 ? 3 : 1;
            soma += digito * peso;
        }

        var verificador = (10 - soma % 10) % 10;
        return verificador == codigo[^1] - '0';
    }

    public override string ToString() => Valor;
}
