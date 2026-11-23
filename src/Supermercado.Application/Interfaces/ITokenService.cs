using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;

namespace Supermercado.Application.Interfaces;

/// <summary>Emissão do token de acesso. A implementação (JWT) fica na Infrastructure.</summary>
public interface ITokenService
{
    TokenAcesso GerarToken(Usuario usuario);
}
