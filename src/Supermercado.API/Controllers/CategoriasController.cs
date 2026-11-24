using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermercado.API.Authorization;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;

namespace Supermercado.API.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize(Policy = Politicas.GerenteOuOperador)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categorias;

    public CategoriasController(ICategoriaService categorias)
    {
        _categorias = categorias;
    }

    /// <summary>Cria uma categoria. Somente Gerente.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.ApenasGerente)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoriaDto>> Criar(
        CriarCategoriaRequest request, CancellationToken cancellationToken)
    {
        var categoria = await _categorias.CriarAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = categoria.Id }, categoria);
    }

    /// <summary>Lista as categorias, em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoriaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await _categorias.ListarAsync(cancellationToken));

    /// <summary>Obtém uma categoria pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaDto>> ObterPorId(Guid id, CancellationToken cancellationToken) =>
        Ok(await _categorias.ObterPorIdAsync(id, cancellationToken));
}
