using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Formato do e-mail: erro do cliente (400), não revela se o e-mail existe.
        RuleFor(x => x.Email)
            .Custom((valor, contexto) =>
            {
                if (!Email.TryCriar(valor, out _, out var erro))
                    contexto.AddFailure(erro);
            });

        // No login só exige que haja senha: a política de tamanho vale para o CADASTRO, não aqui.
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
