using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.Entities;

public class Categoria : Entity
{
    public const int NomeTamanhoMaximo = 100;

    public Categoria(string nome)
    {
        Nome = ValidarNome(nome);
    }

    public string Nome { get; private set; }

    public void Renomear(string novoNome) => Nome = ValidarNome(novoNome);

    private static string ValidarNome(string? nome)
    {
        var normalizado = nome?.Trim();

        if (string.IsNullOrEmpty(normalizado))
            throw new DomainException("O nome da categoria é obrigatório.");

        if (normalizado.Length > NomeTamanhoMaximo)
            throw new DomainException($"O nome da categoria deve ter no máximo {NomeTamanhoMaximo} caracteres.");

        return normalizado;
    }
}
