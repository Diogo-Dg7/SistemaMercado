namespace Supermercado.API.IntegrationTests.Infrastructure;

/// <summary>Gera códigos EAN-13 válidos (com dígito verificador) e únicos dentro da execução.</summary>
public static class GeradorCodigoBarras
{
    private static long _contador = 789_000_000_000;

    public static string Proximo()
    {
        var corpo = Interlocked.Increment(ref _contador).ToString(); // 12 dígitos
        return corpo + DigitoVerificador(corpo);
    }

    private static int DigitoVerificador(string corpo)
    {
        var soma = 0;
        for (var i = corpo.Length - 1; i >= 0; i--)
        {
            var peso = (corpo.Length - 1 - i) % 2 == 0 ? 3 : 1;
            soma += (corpo[i] - '0') * peso;
        }

        return (10 - soma % 10) % 10;
    }
}
