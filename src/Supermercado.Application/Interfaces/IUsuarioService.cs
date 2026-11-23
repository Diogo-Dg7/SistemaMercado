using Supermercado.Application.DTOs;

namespace Supermercado.Application.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default);
}
