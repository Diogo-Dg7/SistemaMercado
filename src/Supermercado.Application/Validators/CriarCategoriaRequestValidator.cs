using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;

namespace Supermercado.Application.Validators;

public class CriarCategoriaRequestValidator : AbstractValidator<CriarCategoriaRequest>
{
    public CriarCategoriaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome da categoria é obrigatório.")
            .MaximumLength(Categoria.NomeTamanhoMaximo)
            .WithMessage($"O nome da categoria deve ter no máximo {Categoria.NomeTamanhoMaximo} caracteres.");
    }
}
