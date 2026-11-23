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
        var erro = Validar(valor, out var normalizado);

        if (erro is not null)
            throw new DomainException(erro);

        Valor = normalizado!;
    }

    public string Valor { get; }

    /// <summary>
    /// Tenta criar o código sem lançar exceção; <paramref name="erro"/> traz o motivo da recusa.
    /// Permite que camadas externas validem usando a mesma regra e mensagens do domínio.
    /// </summary>
    public static bool TryCriar(string? valor, out CodigoBarras? codigo, out string? erro)
    {
        erro = Validar(valor, out var normalizado);
        codigo = erro is null ? new CodigoBarras(normalizado!) : null;
        return erro is null;
    }

    private static string? Validar(string? valor, out string? normalizado)
    {
        normalizado = valor?.Trim();

        if (string.IsNullOrEmpty(normalizado))
            return "O código de barras é obrigatório.";

        if (!normalizado.All(char.IsAsciiDigit))
            return "O código de barras deve conter apenas dígitos.";

        if (!TamanhosValidos.Contains(normalizado.Length))
            return "O código de barras deve ter 8, 12, 13 ou 14 dígitos.";

        if (!DigitoVerificadorValido(normalizado))
            return "O dígito verificador do código de barras é inválido.";

        return null;
    }

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
