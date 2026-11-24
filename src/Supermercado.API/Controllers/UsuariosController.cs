using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermercado.API.Authorization;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;

namespace Supermercado.API.Controllers;

/// <summary>Administração de usuários: somente Gerente.</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.ApenasGerente)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    /// <summary>Cria um usuário. A senha precisa ter no mínimo 8 caracteres e é guardada apenas como hash.</summary>
    [HttpPost]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UsuarioDto>> Criar(CriarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.CriarAsync(request, cancellationToken);

        // Não há "obter por id": o Location aponta para a listagem.
        return CreatedAtAction(nameof(Listar), usuario);
    }

    /// <summary>Lista os usuários (sem o hash da senha), em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await _usuarios.ListarAsync(cancellationToken));
}
