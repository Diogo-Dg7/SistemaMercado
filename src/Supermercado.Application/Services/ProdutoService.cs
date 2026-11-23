using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Mappings;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Services;

public class ProdutoService : IProdutoService
{
    private const string NomeRecurso = "Produto";

    private readonly IProdutoRepository _produtos;
    private readonly ICategoriaRepository _categorias;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IValidator<CriarProdutoRequest> _criarValidator;
    private readonly IValidator<AtualizarPrecoRequest> _atualizarPrecoValidator;
    private readonly IValidator<MovimentarEstoqueRequest> _movimentarEstoqueValidator;

    public ProdutoService(
        IProdutoRepository produtos,
        ICategoriaRepository categorias,
        IUnitOfWork unitOfWork,
        IUsuarioAtual usuarioAtual,
        IValidator<CriarProdutoRequest> criarValidator,
        IValidator<AtualizarPrecoRequest> atualizarPrecoValidator,
        IValidator<MovimentarEstoqueRequest> movimentarEstoqueValidator)
    {
        _produtos = produtos;
        _categorias = categorias;
        _unitOfWork = unitOfWork;
        _usuarioAtual = usuarioAtual;
        _criarValidator = criarValidator;
        _atualizarPrecoValidator = atualizarPrecoValidator;
        _movimentarEstoqueValidator = movimentarEstoqueValidator;
    }

    public async Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, CancellationToken cancellationToken = default)
    {
        await _criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Com rastreamento: o contexto precisa saber que a categoria já existe ao inserir o produto.
        var categoria = await _categorias.ObterParaAlteracaoAsync(request.CategoriaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Categoria", request.CategoriaId);

        var codigoBarras = new CodigoBarras(request.CodigoBarras);

        if (await _produtos.ObterPorCodigoBarrasAsync(codigoBarras, cancellationToken) is not null)
            throw new DomainException($"Já existe um produto com o código de barras {codigoBarras}.");

        var produto = new Produto(
            request.Nome,
            codigoBarras,
            new Dinheiro(request.PrecoCusto),
            new Dinheiro(request.PrecoVenda),
            categoria,
            request.UnidadeMedida,
            request.EstoqueMinimo,
            request.EstoqueInicial);

        await _produtos.AdicionarAsync(produto, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return produto.ToDto();
    }

    public async Task<IReadOnlyList<ProdutoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _produtos.ListarAsync(cancellationToken);

        return produtos.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProdutoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _produtos.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException(NomeRecurso, id);

        return produto.ToDto();
    }

    public async Task<ProdutoDto> AtualizarPrecoAsync(
        Guid id, AtualizarPrecoRequest request, CancellationToken cancellationToken = default)
    {
        await _atualizarPrecoValidator.ValidateAndThrowAsync(request, cancellationToken);

        var produto = await ObterProdutoParaAlteracaoAsync(id, cancellationToken);

        produto.AtualizarPreco(
            new Dinheiro(request.PrecoCusto),
            new Dinheiro(request.PrecoVenda),
            _usuarioAtual.Perfil);

        return await SalvarAsync(produto, cancellationToken);
    }

    public Task<ProdutoDto> AdicionarEstoqueAsync(
        Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken = default) =>
        MovimentarEstoqueAsync(id, request, (produto, quantidade) => produto.AdicionarEstoque(quantidade), cancellationToken);

    public Task<ProdutoDto> BaixarEstoqueAsync(
        Guid id, MovimentarEstoqueRequest request, CancellationToken cancellationToken = default) =>
        MovimentarEstoqueAsync(id, request, (produto, quantidade) => produto.BaixarEstoque(quantidade), cancellationToken);

    public async Task<IReadOnlyList<ProdutoDto>> ListarAbaixoDoEstoqueMinimoAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _produtos.ListarAbaixoDoEstoqueMinimoAsync(cancellationToken);

        return produtos.Select(p => p.ToDto()).ToList();
    }

    private async Task<ProdutoDto> MovimentarEstoqueAsync(
        Guid id,
        MovimentarEstoqueRequest request,
        Action<Produto, decimal> movimentar,
        CancellationToken cancellationToken)
    {
        await _movimentarEstoqueValidator.ValidateAndThrowAsync(request, cancellationToken);

        var produto = await ObterProdutoParaAlteracaoAsync(id, cancellationToken);

        movimentar(produto, request.Quantidade);

        return await SalvarAsync(produto, cancellationToken);
    }

    // Fluxos de escrita leem COM rastreamento: o change tracker detecta o que o domínio alterou e a
    // gravação é protegida pelo controle de concorrência (sem "Atualizar" explícito).
    private async Task<Produto> ObterProdutoParaAlteracaoAsync(Guid id, CancellationToken cancellationToken) =>
        await _produtos.ObterParaAlteracaoAsync(id, cancellationToken)
        ?? throw new RecursoNaoEncontradoException(NomeRecurso, id);

    private async Task<ProdutoDto> SalvarAsync(Produto produto, CancellationToken cancellationToken)
    {
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return produto.ToDto();
    }
}
