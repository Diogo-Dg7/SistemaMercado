using FluentValidation;
using Supermercado.Application.DTOs;

namespace Supermercado.Application.Validators;

public class AtualizarPrecoRequestValidator : AbstractValidator<AtualizarPrecoRequest>
{
    public AtualizarPrecoRequestValidator()
    {
        // A regra "venda >= custo" depende do perfil do usuário e fica no domínio (Produto.AtualizarPreco).
        RuleFor(x => x.PrecoCusto)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço de custo não pode ser negativo.");

        RuleFor(x => x.PrecoVenda)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço de venda não pode ser negativo.");
    }
}
