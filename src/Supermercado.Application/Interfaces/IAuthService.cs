using Supermercado.Application.DTOs;

namespace Supermercado.Application.Interfaces;

public interface IAuthService
{
    /// <exception cref="Exceptions.CredenciaisInvalidasException">
    /// E-mail inexistente, senha errada ou usuário inativo (sempre a mesma exceção e mensagem).
    /// </exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
