namespace Supermercado.Domain.Exceptions;

/// <summary>
/// Lançada ao tentar baixar do estoque mais do que o disponível.
/// </summary>
public class EstoqueInsuficienteException : DomainException
{
    public EstoqueInsuficienteException(int quantidadeDisponivel, int quantidadeSolicitada)
        : base($"Estoque insuficiente: disponível {quantidadeDisponivel}, solicitado {quantidadeSolicitada}.")
    {
        QuantidadeDisponivel = quantidadeDisponivel;
        QuantidadeSolicitada = quantidadeSolicitada;
    }

    public int QuantidadeDisponivel { get; }

    public int QuantidadeSolicitada { get; }
}
