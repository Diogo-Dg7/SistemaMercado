namespace Supermercado.Domain.Exceptions;

/// <summary>
/// Lançada ao tentar baixar do estoque mais do que o disponível.
/// </summary>
public class EstoqueInsuficienteException : DomainException
{
    public EstoqueInsuficienteException(decimal quantidadeDisponivel, decimal quantidadeSolicitada)
        : base($"Estoque insuficiente: disponível {quantidadeDisponivel}, solicitado {quantidadeSolicitada}.")
    {
        QuantidadeDisponivel = quantidadeDisponivel;
        QuantidadeSolicitada = quantidadeSolicitada;
    }

    public decimal QuantidadeDisponivel { get; }

    public decimal QuantidadeSolicitada { get; }
}
