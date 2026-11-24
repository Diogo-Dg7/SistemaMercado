using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Security;
using Supermercado.Domain.Enums;

namespace Supermercado.API.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Autentica e devolve o token JWT (válido por 8 horas). E-mail inexistente, senha errada e usuário
    /// inativo recebem sempre a mesma resposta 401.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _auth.LoginAsync(request, cancellationToken));

    /// <summary>Dados do usuário autenticado, conforme o token.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UsuarioLogadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioLogadoDto> Me()
    {
        var perfil = User.FindFirst(NomesDeClaim.Perfil)?.Value;

        if (!Guid.TryParse(User.FindFirst(NomesDeClaim.Id)?.Value, out var id)
            || !Enum.TryParse<PerfilUsuario>(perfil, out var perfilUsuario))
            return Unauthorized();

        return Ok(new UsuarioLogadoDto(
            id,
            User.FindFirst(NomesDeClaim.Nome)?.Value ?? "",
            User.FindFirst(NomesDeClaim.Email)?.Value ?? "",
            perfilUsuario));
    }
}
