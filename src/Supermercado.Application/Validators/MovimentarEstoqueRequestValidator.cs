using FluentValidation;
using Supermercado.Application.DTOs;

namespace Supermercado.Application.Validators;

public class MovimentarEstoqueRequestValidator : AbstractValidator<MovimentarEstoqueRequest>
{
    public MovimentarEstoqueRequestValidator()
    {
        // A precisão da quantidade (inteira ou até 3 casas) depende da unidade do produto e fica no domínio.
        RuleFor(x => x.Quantidade)
            .GreaterThan(0m).WithMessage("A quantidade deve ser maior que zero.");
    }
}
