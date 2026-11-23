using Supermercado.Application.DTOs;

namespace Supermercado.Application.Interfaces;

public interface ICategoriaService
{
    Task<CategoriaDto> CriarAsync(CriarCategoriaRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.RecursoNaoEncontradoException">A categoria não existe.</exception>
    Task<CategoriaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
}
