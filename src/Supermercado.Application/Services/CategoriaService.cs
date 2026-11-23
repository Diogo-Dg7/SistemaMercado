using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Mappings;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Interfaces;

namespace Supermercado.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _categorias;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CriarCategoriaRequest> _criarValidator;

    public CategoriaService(
        ICategoriaRepository categorias,
        IUnitOfWork unitOfWork,
        IValidator<CriarCategoriaRequest> criarValidator)
    {
        _categorias = categorias;
        _unitOfWork = unitOfWork;
        _criarValidator = criarValidator;
    }

    public async Task<CategoriaDto> CriarAsync(CriarCategoriaRequest request, CancellationToken cancellationToken = default)
    {
        await _criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var categoria = new Categoria(request.Nome);

        await _categorias.AdicionarAsync(categoria, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return categoria.ToDto();
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var categorias = await _categorias.ListarAsync(cancellationToken);

        return categorias.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoriaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _categorias.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Categoria", id);

        return categoria.ToDto();
    }
}
