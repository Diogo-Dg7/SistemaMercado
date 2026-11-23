namespace Supermercado.Application.Exceptions;

/// <summary>
/// O registro foi alterado por outra operação entre a leitura e a gravação (concorrência otimista).
/// Será traduzida para HTTP 409; o cliente deve reler o recurso e tentar de novo.
/// </summary>
public class ConflitoDeConcorrenciaException : Exception
{
    public ConflitoDeConcorrenciaException(Exception? innerException = null)
        : base("O registro foi alterado por outra operação. Recarregue os dados e tente novamente.", innerException)
    {
    }
}
