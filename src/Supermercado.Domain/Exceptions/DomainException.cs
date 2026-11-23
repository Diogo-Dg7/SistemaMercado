namespace Supermercado.Domain.Exceptions;

/// <summary>
/// Violação de uma regra de negócio ou invariante do domínio.
/// Tratada no ExceptionHandlingMiddleware da API (ProblemDetails).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
