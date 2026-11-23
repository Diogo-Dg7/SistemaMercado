using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Domain.Entities;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Validators;

public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public const int SenhaTamanhoMinimo = 8;
    public const int SenhaTamanhoMaximo = 128;

    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do usuário é obrigatório.")
            .MaximumLength(Usuario.NomeTamanhoMaximo)
            .WithMessage($"O nome do usuário deve ter no máximo {Usuario.NomeTamanhoMaximo} caracteres.");

        // Reaproveita a regra e as mensagens do value object.
        RuleFor(x => x.Email)
            .Custom((valor, contexto) =>
            {
                if (!Email.TryCriar(valor, out _, out var erro))
                    contexto.AddFailure(erro);
            });

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(SenhaTamanhoMinimo).WithMessage($"A senha deve ter no mínimo {SenhaTamanhoMinimo} caracteres.")
            .MaximumLength(SenhaTamanhoMaximo).WithMessage($"A senha deve ter no máximo {SenhaTamanhoMaximo} caracteres.");

        RuleFor(x => x.Perfil)
            .IsInEnum().WithMessage("O perfil do usuário é inválido.");
    }
}
