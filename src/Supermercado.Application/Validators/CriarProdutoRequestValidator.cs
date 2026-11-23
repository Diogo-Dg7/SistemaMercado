using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Validators;

public class CriarProdutoRequestValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do produto é obrigatório.")
            .MaximumLength(Produto.NomeTamanhoMaximo)
            .WithMessage($"O nome do produto deve ter no máximo {Produto.NomeTamanhoMaximo} caracteres.");

        // Reaproveita a regra e as mensagens do value object, sem duplicá-las aqui.
        RuleFor(x => x.CodigoBarras)
            .Custom((valor, contexto) =>
            {
                if (!CodigoBarras.TryCriar(valor, out _, out var erro))
                    contexto.AddFailure(erro);
            });

        RuleFor(x => x.PrecoCusto)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço de custo não pode ser negativo.");

        RuleFor(x => x.PrecoVenda)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço de venda não pode ser negativo.");

        RuleFor(x => x.CategoriaId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.UnidadeMedida)
            .IsInEnum().WithMessage("A unidade de medida é inválida.");

        RuleFor(x => x.EstoqueMinimo)
            .GreaterThanOrEqualTo(0m).WithMessage("O estoque mínimo não pode ser negativo.");

        RuleFor(x => x.EstoqueInicial)
            .GreaterThanOrEqualTo(0m).WithMessage("O estoque inicial não pode ser negativo.");
    }
}
