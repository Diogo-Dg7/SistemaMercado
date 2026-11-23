using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Tests.Entities;

public class ProdutoTests
{
    private static readonly Categoria CategoriaPadrao = new("Bebidas");
    private static readonly CodigoBarras CodigoPadrao = new("4006381333931");

    private static Produto CriarProduto(
        string nome = "Refrigerante 2L",
        decimal custo = 5m,
        decimal venda = 8m,
        int estoqueMinimo = 10,
        int estoqueInicial = 20) =>
        new(nome, CodigoPadrao, new Dinheiro(custo), new Dinheiro(venda), CategoriaPadrao, estoqueMinimo, estoqueInicial);

    // ---------- Criação ----------

    [Fact]
    public void Deve_criar_produto_valido()
    {
        var produto = CriarProduto();

        produto.Id.Should().NotBeEmpty();
        produto.Nome.Should().Be("Refrigerante 2L");
        produto.CodigoBarras.Should().Be(CodigoPadrao);
        produto.PrecoCusto.Should().Be(new Dinheiro(5m));
        produto.PrecoVenda.Should().Be(new Dinheiro(8m));
        produto.Estoque.Should().Be(20);
        produto.EstoqueMinimo.Should().Be(10);
        produto.Categoria.Should().BeSameAs(CategoriaPadrao);
        produto.CategoriaId.Should().Be(CategoriaPadrao.Id);
    }

    [Fact]
    public void Estoque_e_minimo_devem_ter_padrao_zero()
    {
        var produto = new Produto("Arroz", CodigoPadrao, new Dinheiro(3m), new Dinheiro(5m), CategoriaPadrao);

        produto.Estoque.Should().Be(0);
        produto.EstoqueMinimo.Should().Be(0);
    }

    [Fact]
    public void Deve_remover_espacos_do_nome()
    {
        CriarProduto(nome: "  Arroz ").Nome.Should().Be("Arroz");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_nome_vazio(string? nome)
    {
        var act = () => CriarProduto(nome: nome!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_nome_maior_que_o_limite()
    {
        var act = () => CriarProduto(nome: new string('a', Produto.NomeTamanhoMaximo + 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_preco_de_venda_menor_que_o_custo()
    {
        var act = () => CriarProduto(custo: 10m, venda: 9.99m);

        act.Should().Throw<DomainException>().WithMessage("*menor que o preço de custo*");
    }

    [Fact]
    public void Deve_aceitar_preco_de_venda_igual_ao_custo()
    {
        var act = () => CriarProduto(custo: 10m, venda: 10m);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_rejeitar_estoque_minimo_negativo()
    {
        var act = () => CriarProduto(estoqueMinimo: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_estoque_inicial_negativo()
    {
        var act = () => CriarProduto(estoqueInicial: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deve_rejeitar_dependencias_nulas()
    {
        var preco = new Dinheiro(1m);

        ((Action)(() => new Produto("A", null!, preco, preco, CategoriaPadrao))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new Produto("A", CodigoPadrao, null!, preco, CategoriaPadrao))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new Produto("A", CodigoPadrao, preco, null!, CategoriaPadrao))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new Produto("A", CodigoPadrao, preco, preco, null!))).Should().Throw<ArgumentNullException>();
    }

    // ---------- AtualizarPreco ----------

    [Fact]
    public void AtualizarPreco_deve_alterar_custo_e_venda()
    {
        var produto = CriarProduto();

        produto.AtualizarPreco(new Dinheiro(6m), new Dinheiro(9m), PerfilUsuario.Operador);

        produto.PrecoCusto.Should().Be(new Dinheiro(6m));
        produto.PrecoVenda.Should().Be(new Dinheiro(9m));
    }

    [Fact]
    public void AtualizarPreco_operador_nao_pode_vender_abaixo_do_custo()
    {
        var produto = CriarProduto();

        var act = () => produto.AtualizarPreco(new Dinheiro(10m), new Dinheiro(9m), PerfilUsuario.Operador);

        act.Should().Throw<DomainException>();
        produto.PrecoCusto.Should().Be(new Dinheiro(5m), "o estado não deve mudar quando a regra é violada");
        produto.PrecoVenda.Should().Be(new Dinheiro(8m));
    }

    [Fact]
    public void AtualizarPreco_gerente_pode_vender_abaixo_do_custo()
    {
        var produto = CriarProduto();

        produto.AtualizarPreco(new Dinheiro(10m), new Dinheiro(9m), PerfilUsuario.Gerente);

        produto.PrecoCusto.Should().Be(new Dinheiro(10m));
        produto.PrecoVenda.Should().Be(new Dinheiro(9m));
    }

    [Fact]
    public void AtualizarPreco_deve_rejeitar_precos_nulos()
    {
        var produto = CriarProduto();

        ((Action)(() => produto.AtualizarPreco(null!, new Dinheiro(1m), PerfilUsuario.Gerente))).Should().Throw<ArgumentNullException>();
        ((Action)(() => produto.AtualizarPreco(new Dinheiro(1m), null!, PerfilUsuario.Gerente))).Should().Throw<ArgumentNullException>();
    }

    // ---------- AdicionarEstoque ----------

    [Fact]
    public void AdicionarEstoque_deve_somar_quantidade()
    {
        var produto = CriarProduto(estoqueInicial: 5);

        produto.AdicionarEstoque(7);

        produto.Estoque.Should().Be(12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AdicionarEstoque_deve_rejeitar_quantidade_nao_positiva(int quantidade)
    {
        var produto = CriarProduto(estoqueInicial: 5);

        var act = () => produto.AdicionarEstoque(quantidade);

        act.Should().Throw<DomainException>();
        produto.Estoque.Should().Be(5);
    }

    [Fact]
    public void AdicionarEstoque_deve_rejeitar_estouro_de_capacidade()
    {
        var produto = CriarProduto(estoqueInicial: int.MaxValue);

        var act = () => produto.AdicionarEstoque(1);

        act.Should().Throw<DomainException>();
        produto.Estoque.Should().Be(int.MaxValue);
    }

    // ---------- BaixarEstoque ----------

    [Fact]
    public void BaixarEstoque_deve_subtrair_quantidade()
    {
        var produto = CriarProduto(estoqueInicial: 10);

        produto.BaixarEstoque(4);

        produto.Estoque.Should().Be(6);
    }

    [Fact]
    public void BaixarEstoque_pode_zerar_o_estoque()
    {
        var produto = CriarProduto(estoqueInicial: 10);

        produto.BaixarEstoque(10);

        produto.Estoque.Should().Be(0);
    }

    [Fact]
    public void BaixarEstoque_acima_do_disponivel_deve_lancar_EstoqueInsuficienteException()
    {
        var produto = CriarProduto(estoqueInicial: 3);

        var act = () => produto.BaixarEstoque(4);

        act.Should().Throw<EstoqueInsuficienteException>()
            .Where(e => e.QuantidadeDisponivel == 3 && e.QuantidadeSolicitada == 4);
        produto.Estoque.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BaixarEstoque_deve_rejeitar_quantidade_nao_positiva(int quantidade)
    {
        var produto = CriarProduto(estoqueInicial: 5);

        var act = () => produto.BaixarEstoque(quantidade);

        act.Should().Throw<DomainException>();
        produto.Estoque.Should().Be(5);
    }

    // ---------- EstaAbaixoDoEstoqueMinimo ----------

    [Theory]
    [InlineData(9, 10, true)]
    [InlineData(10, 10, false)]
    [InlineData(11, 10, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, true)]
    public void EstaAbaixoDoEstoqueMinimo_deve_comparar_estoque_com_minimo(int estoque, int minimo, bool esperado)
    {
        var produto = CriarProduto(estoqueMinimo: minimo, estoqueInicial: estoque);

        produto.EstaAbaixoDoEstoqueMinimo().Should().Be(esperado);
    }

    [Fact]
    public void EstaAbaixoDoEstoqueMinimo_deve_refletir_movimentacoes()
    {
        var produto = CriarProduto(estoqueMinimo: 10, estoqueInicial: 12);
        produto.EstaAbaixoDoEstoqueMinimo().Should().BeFalse();

        produto.BaixarEstoque(3);
        produto.EstaAbaixoDoEstoqueMinimo().Should().BeTrue();

        produto.AdicionarEstoque(5);
        produto.EstaAbaixoDoEstoqueMinimo().Should().BeFalse();
    }
}
