using Supermercado.Application.Interfaces;
using Supermercado.Application.Security;
using Supermercado.Domain.Enums;

namespace Supermercado.API.Services;

/// <summary>Perfil do usuário da requisição atual, lido da claim "role" do JWT já validado.</summary>
public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtual(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public PerfilUsuario Perfil
    {
        get
        {
            var valor = _httpContextAccessor.HttpContext?.User.FindFirst(NomesDeClaim.Perfil)?.Value;

            if (Enum.TryParse<PerfilUsuario>(valor, ignoreCase: false, out var perfil) && Enum.IsDefined(perfil))
                return perfil;

            // Os endpoints são [Authorize]; chegar aqui sem perfil é erro de configuração, não de quem chama.
            // Falha explícita em vez de assumir um perfil.
            throw new InvalidOperationException("Não há um usuário autenticado com perfil válido na requisição atual.");
        }
    }
}
