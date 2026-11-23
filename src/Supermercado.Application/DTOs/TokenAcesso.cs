namespace Supermercado.Application.DTOs;

/// <summary>Token emitido e o instante (UTC) em que expira.</summary>
public sealed record TokenAcesso(string Token, DateTime ExpiraEm);
