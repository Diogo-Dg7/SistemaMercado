using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Services;
using Supermercado.Application.Validators;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Tests.Services;

public class ProdutoServiceTests
{
    private const string CodigoValido = "4006381333931";

    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioAtual _usuarioAtual = Substitute.For<IUsuarioAtual>();
    private readonly ProdutoService _service;
    private readonly Categoria _categoria = new("Bebidas");

    public ProdutoServiceTests()
    {
        _usuarioAtual.Perfil.Returns(PerfilUsuario.Operador);
        _categorias.ObterPorIdAsync(_categoria.Id, Arg.Any<CancellationToken>()).Returns(_categoria);

        _service = new ProdutoService(
            _produtos,
            _categorias,
            _unitOfWork,
            _usuarioAtual,
            new CriarProdutoRequestValidator(),
            new AtualizarPrecoRequestValidator(),
            new MovimentarEstoqueRequestValidator());
    }

    private Produto ProdutoExistente(
        decimal custo = 5m,
        decimal venda = 8m,
        decimal estoqueMinimo = 10m,
        decimal estoque = 20m,
        UnidadeMedida unidade = UnidadeMedida.Unidade)
    {
        var produto = new Produto(
            "Refrigerante 2L", new CodigoBarras(CodigoValido), new Dinheiro(custo), new Dinheiro(venda),
            _categoria, unidade, estoqueMinimo, estoque);
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);
        return produto;
    }

    private CriarProdutoRequest RequestValido(string codigo = CodigoValido, Guid? categoriaId = null) =>
        new("Refrigerante 2L", codigo, 5m, 8m, categoriaId ?? _categoria.Id, UnidadeMedida.Unidade, 10m, 20m);

    private async Task DeveTerSalvadoUmaVez() =>
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());

    private async Task NaoDeveTerSalvado() =>
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(default);

    // ---------- Criar ----------

    [Fact]
    public async Task Criar_deve_adicionar_salvar_e_retornar_dto()
    {
        var dto = await _service.CriarAsync(RequestValido());

        dto.Nome.Should().Be("Refrigerante 2L");
        dto.CodigoBarras.Should().Be(CodigoValido);
        dto.PrecoCusto.Should().Be(5m);
        dto.PrecoVenda.Should().Be(8m);
        dto.Estoque.Should().Be(20m);
        dto.EstoqueMinimo.Should().Be(10m);
        dto.UnidadeMedida.Should().Be(UnidadeMedida.Unidade);
        dto.CategoriaId.Should().Be(_categoria.Id);
        dto.AbaixoDoEstoqueMinimo.Should().BeFalse();
        await _produtos.Received(1).AdicionarAsync(
            Arg.Is<Produto>(p => p.Id == dto.Id), Arg.Any<CancellationToken>());
        await DeveTerSalvadoUmaVez();
    }

    [Fact]
    public async Task Criar_com_categoria_inexistente_deve_lancar_RecursoNaoEncontradoException()
    {
        var idInexistente = Guid.NewGuid();
        _categorias.ObterPorIdAsync(idInexistente, Arg.Any<CancellationToken>()).Returns((Categoria?)null);

        var act = () => _service.CriarAsync(RequestValido(categoriaId: idInexistente));

        (await act.Should().ThrowAsync<RecursoNaoEncontradoException>()).Which.Recurso.Should().Be("Categoria");
        await _produtos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task Criar_com_request_invalido_deve_lancar_ValidationException_sem_persistir()
    {
        var act = () => _service.CriarAsync(RequestValido(codigo: "123"));

        await act.Should().ThrowAsync<ValidationException>();
        await _produtos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task Criar_com_codigo_de_barras_ja_cadastrado_deve_lancar_DomainException()
    {
        var existente = ProdutoExistente();
        _produtos.ObterPorCodigoBarrasAsync(new CodigoBarras(CodigoValido), Arg.Any<CancellationToken>())
            .Returns(existente);

        var act = () => _service.CriarAsync(RequestValido());

        await act.Should().ThrowAsync<DomainException>().WithMessage("*código de barras*");
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task Criar_com_preco_de_venda_abaixo_do_custo_deve_lancar_DomainException()
    {
        var request = RequestValido() with { PrecoCusto = 10m, PrecoVenda = 9m };

        var act = () => _service.CriarAsync(request);

        await act.Should().ThrowAsync<DomainException>();
        await NaoDeveTerSalvado();
    }

    // ---------- Listar / Obter ----------

    [Fact]
    public async Task Listar_deve_retornar_dtos()
    {
        var produto = ProdutoExistente();
        _produtos.ListarAsync(Arg.Any<CancellationToken>()).Returns(new List<Produto> { produto });

        var dtos = await _service.ListarAsync();

        dtos.Should().ContainSingle().Which.Id.Should().Be(produto.Id);
    }

    [Fact]
    public async Task ObterPorId_deve_retornar_dto()
    {
        var produto = ProdutoExistente();

        var dto = await _service.ObterPorIdAsync(produto.Id);

        dto.Id.Should().Be(produto.Id);
        dto.Nome.Should().Be("Refrigerante 2L");
    }

    [Fact]
    public async Task ObterPorId_inexistente_deve_lancar_RecursoNaoEncontradoException()
    {
        var id = Guid.NewGuid();
        _produtos.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((Produto?)null);

        var act = () => _service.ObterPorIdAsync(id);

        (await act.Should().ThrowAsync<RecursoNaoEncontradoException>()).Which.Recurso.Should().Be("Produto");
    }

    // ---------- AtualizarPreco ----------

    [Fact]
    public async Task AtualizarPreco_deve_atualizar_salvar_e_retornar_dto()
    {
        var produto = ProdutoExistente();

        var dto = await _service.AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(6m, 9m));

        dto.PrecoCusto.Should().Be(6m);
        dto.PrecoVenda.Should().Be(9m);
        _produtos.Received(1).Atualizar(produto);
        await DeveTerSalvadoUmaVez();
    }

    [Fact]
    public async Task AtualizarPreco_operador_com_venda_abaixo_do_custo_deve_lancar_DomainException()
    {
        _usuarioAtual.Perfil.Returns(PerfilUsuario.Operador);
        var produto = ProdutoExistente();

        var act = () => _service.AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(10m, 9m));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*menor que o preço de custo*");
        produto.PrecoVenda.Valor.Should().Be(8m);
        _produtos.DidNotReceiveWithAnyArgs().Atualizar(default!);
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task AtualizarPreco_gerente_com_venda_abaixo_do_custo_deve_permitir()
    {
        _usuarioAtual.Perfil.Returns(PerfilUsuario.Gerente);
        var produto = ProdutoExistente();

        var dto = await _service.AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(10m, 9m));

        dto.PrecoCusto.Should().Be(10m);
        dto.PrecoVenda.Should().Be(9m);
        await DeveTerSalvadoUmaVez();
    }

    [Fact]
    public async Task AtualizarPreco_inexistente_deve_lancar_RecursoNaoEncontradoException()
    {
        var id = Guid.NewGuid();
        _produtos.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((Produto?)null);

        var act = () => _service.AtualizarPrecoAsync(id, new AtualizarPrecoRequest(1m, 2m));

        await act.Should().ThrowAsync<RecursoNaoEncontradoException>();
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task AtualizarPreco_com_request_invalido_deve_lancar_ValidationException()
    {
        var produto = ProdutoExistente();

        var act = () => _service.AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(-1m, 2m));

        await act.Should().ThrowAsync<ValidationException>();
        await NaoDeveTerSalvado();
    }

    // ---------- Estoque ----------

    [Fact]
    public async Task AdicionarEstoque_deve_somar_salvar_e_retornar_dto()
    {
        var produto = ProdutoExistente(estoque: 20m);

        var dto = await _service.AdicionarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(5m));

        dto.Estoque.Should().Be(25m);
        _produtos.Received(1).Atualizar(produto);
        await DeveTerSalvadoUmaVez();
    }

    [Fact]
    public async Task BaixarEstoque_deve_subtrair_salvar_e_retornar_dto()
    {
        var produto = ProdutoExistente(estoque: 20m);

        var dto = await _service.BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(5m));

        dto.Estoque.Should().Be(15m);
        _produtos.Received(1).Atualizar(produto);
        await DeveTerSalvadoUmaVez();
    }

    [Fact]
    public async Task BaixarEstoque_fracionado_de_produto_por_quilo_deve_funcionar()
    {
        var produto = ProdutoExistente(estoque: 10m, unidade: UnidadeMedida.Quilograma);

        var dto = await _service.BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(0.250m));

        dto.Estoque.Should().Be(9.75m);
    }

    [Fact]
    public async Task BaixarEstoque_acima_do_disponivel_deve_lancar_EstoqueInsuficienteException()
    {
        var produto = ProdutoExistente(estoque: 3m);

        var act = () => _service.BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(4m));

        await act.Should().ThrowAsync<EstoqueInsuficienteException>();
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task AdicionarEstoque_fracionado_de_produto_por_unidade_deve_lancar_DomainException()
    {
        var produto = ProdutoExistente(unidade: UnidadeMedida.Unidade);

        var act = () => _service.AdicionarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(1.5m));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inteiras*");
        await NaoDeveTerSalvado();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Movimentar_estoque_com_quantidade_invalida_deve_lancar_ValidationException(int quantidade)
    {
        var produto = ProdutoExistente();

        var adicionar = () => _service.AdicionarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(quantidade));
        var baixar = () => _service.BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(quantidade));

        await adicionar.Should().ThrowAsync<ValidationException>();
        await baixar.Should().ThrowAsync<ValidationException>();
        await NaoDeveTerSalvado();
    }

    [Fact]
    public async Task Movimentar_estoque_de_produto_inexistente_deve_lancar_RecursoNaoEncontradoException()
    {
        var id = Guid.NewGuid();
        _produtos.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((Produto?)null);

        var adicionar = () => _service.AdicionarEstoqueAsync(id, new MovimentarEstoqueRequest(1m));
        var baixar = () => _service.BaixarEstoqueAsync(id, new MovimentarEstoqueRequest(1m));

        await adicionar.Should().ThrowAsync<RecursoNaoEncontradoException>();
        await baixar.Should().ThrowAsync<RecursoNaoEncontradoException>();
        await NaoDeveTerSalvado();
    }

    // ---------- Alerta de reposição ----------

    [Fact]
    public async Task ListarAbaixoDoEstoqueMinimo_deve_retornar_dtos_do_repositorio()
    {
        var produto = ProdutoExistente(estoqueMinimo: 10m, estoque: 2m);
        _produtos.ListarAbaixoDoEstoqueMinimoAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Produto> { produto });

        var dtos = await _service.ListarAbaixoDoEstoqueMinimoAsync();

        var dto = dtos.Should().ContainSingle().Subject;
        dto.Id.Should().Be(produto.Id);
        dto.AbaixoDoEstoqueMinimo.Should().BeTrue();
    }
}
