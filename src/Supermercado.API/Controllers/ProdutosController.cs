using Microsoft.AspNetCore.Mvc;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;

namespace Supermercado.API.Controllers;

[ApiController]
[Route("api/produtos")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtos;

    public ProdutosController(IProdutoService produtos)
    {
        _produtos = produtos;
    }

    /// <summary>Cria um produto.</summary>
    [HttpPost]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> Criar(CriarProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await _produtos.CriarAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    /// <summary>Lista os produtos, em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProdutoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProdutoDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await _produtos.ListarAsync(cancellationToken));

    /// <summary>Lista os produtos com estoque abaixo do mínimo (alerta de reposição).</summary>
    [HttpGet("abaixo-estoque-minimo")]
    [ProducesResponseType<IReadOnlyList<ProdutoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProdutoDto>>> ListarAbaixoDoEstoqueMinimo(
        CancellationToken cancellationToken) =>
        Ok(await _produtos.ListarAbaixoDoEstoqueMinimoAsync(cancellationToken));

    /// <summary>Obtém um produto pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoDto>> ObterPorId(Guid id, CancellationToken cancellationToken) =>
        Ok(await _produtos.ObterPorIdAsync(id, cancellationToken));

    /// <summary>Atualiza os preços de custo e de venda. Só o perfil Gerente pode vender abaixo do custo.</summary>
    [HttpPut("{id:guid}/preco")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> AtualizarPreco(
        Guid id, AtualizarPrecoRequest request, CancellationToken cancellationToken) =>
        Ok(await _produtos.AtualizarPrecoAsync(id, request, cancellationToken));

    /// <summary>Registra uma entrada de estoque.</summary>
    [HttpPost("{id:guid}/estoque/entrada")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> AdicionarEstoque(
        Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken) =>
        Ok(await _produtos.AdicionarEstoqueAsync(id, request, cancellationToken));

    /// <summary>Registra uma saída de estoque. Falha com 409 se a quantidade exceder o saldo.</summary>
    [HttpPost("{id:guid}/estoque/saida")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> BaixarEstoque(
        Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken) =>
        Ok(await _produtos.BaixarEstoqueAsync(id, request, cancellationToken));
}
