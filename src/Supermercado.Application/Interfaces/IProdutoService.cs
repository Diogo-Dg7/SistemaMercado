using Supermercado.Application.DTOs;

namespace Supermercado.Application.Interfaces;

public interface IProdutoService
{
    Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoDto>> ListarAsync(CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.RecursoNaoEncontradoException">O produto não existe.</exception>
    Task<ProdutoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Usa o perfil do usuário atual: só o Gerente pode vender abaixo do custo.</summary>
    Task<ProdutoDto> AtualizarPrecoAsync(Guid id, AtualizarPrecoRequest request, CancellationToken cancellationToken = default);

    Task<ProdutoDto> AdicionarEstoqueAsync(Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken = default);

    Task<ProdutoDto> BaixarEstoqueAsync(Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken = default);

    /// <summary>Produtos para o alerta de reposição.</summary>
    Task<IReadOnlyList<ProdutoDto>> ListarAbaixoDoEstoqueMinimoAsync(CancellationToken cancellationToken = default);
}
