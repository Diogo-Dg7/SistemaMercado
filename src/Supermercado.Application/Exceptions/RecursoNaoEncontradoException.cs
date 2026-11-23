namespace Supermercado.Application.Exceptions;

/// <summary>
/// Recurso solicitado não existe. Será traduzida para HTTP 404 no ExceptionHandlingMiddleware.
/// </summary>
public class RecursoNaoEncontradoException : Exception
{
    public RecursoNaoEncontradoException(string recurso, Guid id)
        : base($"{recurso} '{id}' não encontrado.")
    {
        Recurso = recurso;
        Id = id;
    }

    public string Recurso { get; }

    public Guid Id { get; }
}
