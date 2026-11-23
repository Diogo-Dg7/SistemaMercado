using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IValidator<LoginRequest> loginValidator)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _loginValidator = loginValidator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await _usuarios.ObterPorEmailAsync(new Email(request.Email), cancellationToken);

        if (usuario is null)
        {
            // Gasta o mesmo tempo de um login real (o custo está no hash), para que o tempo de resposta
            // não revele se o e-mail existe.
            _passwordHasher.Gerar(request.Senha);
            throw new CredenciaisInvalidasException();
        }

        var senhaCorreta = _passwordHasher.Verificar(usuario.SenhaHash, request.Senha);

        // Senha errada e usuário inativo são indistinguíveis para quem chama.
        if (!senhaCorreta || !usuario.Ativo)
            throw new CredenciaisInvalidasException();

        var acesso = _tokenService.GerarToken(usuario);

        return new LoginResponse(acesso.Token, acesso.ExpiraEm, usuario.Nome, usuario.Perfil);
    }
}
