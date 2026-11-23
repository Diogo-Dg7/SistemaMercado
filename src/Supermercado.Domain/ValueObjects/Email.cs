using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.ValueObjects;

/// <summary>
/// E-mail imutável, validado e normalizado (sem espaços nas pontas e em minúsculas),
/// de modo que "Ana@Mercado.com " e "ana@mercado.com" sejam o mesmo e-mail.
/// </summary>
public sealed record Email
{
    public const int TamanhoMaximo = 254;
    private const int TamanhoMaximoParteLocal = 64;

    public Email(string valor)
    {
        var erro = Validar(valor, out var normalizado);

        if (erro is not null)
            throw new DomainException(erro);

        Valor = normalizado!;
    }

    public string Valor { get; }

    /// <summary>
    /// Tenta criar o e-mail sem lançar exceção; <paramref name="erro"/> traz o motivo da recusa.
    /// Permite que camadas externas validem usando a mesma regra e mensagens do domínio.
    /// </summary>
    public static bool TryCriar(string? valor, out Email? email, out string? erro)
    {
        erro = Validar(valor, out var normalizado);
        email = erro is null ? new Email(normalizado!) : null;
        return erro is null;
    }

    private static string? Validar(string? valor, out string? normalizado)
    {
        normalizado = valor?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizado))
            return "O e-mail é obrigatório.";

        if (normalizado.Length > TamanhoMaximo)
            return $"O e-mail deve ter no máximo {TamanhoMaximo} caracteres.";

        if (normalizado.Any(char.IsWhiteSpace))
            return "O e-mail é inválido.";

        var arroba = normalizado.IndexOf('@');
        if (arroba <= 0 || arroba != normalizado.LastIndexOf('@'))
            return "O e-mail é inválido.";

        var parteLocal = normalizado[..arroba];
        var dominio = normalizado[(arroba + 1)..];

        if (parteLocal.Length > TamanhoMaximoParteLocal)
            return "O e-mail é inválido.";

        // O domínio precisa de ao menos um ponto com texto dos dois lados ("mercado.com").
        var ponto = dominio.LastIndexOf('.');
        if (ponto <= 0 || ponto == dominio.Length - 1 || dominio.Contains(".."))
            return "O e-mail é inválido.";

        return null;
    }

    public override string ToString() => Valor;
}
