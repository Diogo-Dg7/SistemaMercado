using FluentValidation.TestHelper;
using Supermercado.Application.DTOs;
using Supermercado.Application.Validators;
using Supermercado.Domain.Enums;

namespace Supermercado.Application.Tests.Validators;

public class ValidatorsTests
{
    private static CriarProdutoRequest ProdutoValido() =>
        new("Arroz 5kg", "4006381333931", 10m, 15m, Guid.NewGuid(), UnidadeMedida.Unidade, 5m, 10m);

    // ---------- CriarCategoriaRequest ----------

    [Fact]
    public void CriarCategoria_valida_nao_deve_ter_erros()
    {
        new CriarCategoriaRequestValidator().TestValidate(new CriarCategoriaRequest("Bebidas"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarCategoria_com_nome_vazio_deve_falhar(string nome)
    {
        new CriarCategoriaRequestValidator().TestValidate(new CriarCategoriaRequest(nome))
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void CriarCategoria_com_nome_muito_longo_deve_falhar()
    {
        new CriarCategoriaRequestValidator().TestValidate(new CriarCategoriaRequest(new string('a', 101)))
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    // ---------- CriarProdutoRequest ----------

    [Fact]
    public void CriarProduto_valido_nao_deve_ter_erros()
    {
        new CriarProdutoRequestValidator().TestValidate(ProdutoValido()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CriarProduto_com_nome_vazio_deve_falhar()
    {
        new CriarProdutoRequestValidator().TestValidate(ProdutoValido() with { Nome = "" })
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("123")]
    [InlineData("4006381333932")]
    public void CriarProduto_com_codigo_de_barras_invalido_deve_falhar(string codigo)
    {
        new CriarProdutoRequestValidator().TestValidate(ProdutoValido() with { CodigoBarras = codigo })
            .ShouldHaveValidationErrorFor(x => x.CodigoBarras);
    }

    [Fact]
    public void CriarProduto_com_precos_negativos_deve_falhar()
    {
        var resultado = new CriarProdutoRequestValidator()
            .TestValidate(ProdutoValido() with { PrecoCusto = -1m, PrecoVenda = -1m });

        resultado.ShouldHaveValidationErrorFor(x => x.PrecoCusto);
        resultado.ShouldHaveValidationErrorFor(x => x.PrecoVenda);
    }

    [Fact]
    public void CriarProduto_sem_categoria_deve_falhar()
    {
        new CriarProdutoRequestValidator().TestValidate(ProdutoValido() with { CategoriaId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.CategoriaId);
    }

    [Fact]
    public void CriarProduto_com_unidade_de_medida_invalida_deve_falhar()
    {
        new CriarProdutoRequestValidator().TestValidate(ProdutoValido() with { UnidadeMedida = (UnidadeMedida)99 })
            .ShouldHaveValidationErrorFor(x => x.UnidadeMedida);
    }

    [Fact]
    public void CriarProduto_com_estoques_negativos_deve_falhar()
    {
        var resultado = new CriarProdutoRequestValidator()
            .TestValidate(ProdutoValido() with { EstoqueMinimo = -1m, EstoqueInicial = -1m });

        resultado.ShouldHaveValidationErrorFor(x => x.EstoqueMinimo);
        resultado.ShouldHaveValidationErrorFor(x => x.EstoqueInicial);
    }

    // ---------- AtualizarPrecoRequest ----------

    [Fact]
    public void AtualizarPreco_valido_nao_deve_ter_erros()
    {
        new AtualizarPrecoRequestValidator().TestValidate(new AtualizarPrecoRequest(5m, 8m))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AtualizarPreco_com_precos_negativos_deve_falhar()
    {
        var resultado = new AtualizarPrecoRequestValidator().TestValidate(new AtualizarPrecoRequest(-1m, -1m));

        resultado.ShouldHaveValidationErrorFor(x => x.PrecoCusto);
        resultado.ShouldHaveValidationErrorFor(x => x.PrecoVenda);
    }

    [Fact]
    public void AtualizarPreco_nao_valida_venda_contra_custo_pois_depende_do_perfil()
    {
        new AtualizarPrecoRequestValidator().TestValidate(new AtualizarPrecoRequest(10m, 5m))
            .ShouldNotHaveAnyValidationErrors();
    }

    // ---------- MovimentarEstoqueRequest ----------

    [Theory]
    [InlineData(0.001)]
    [InlineData(1)]
    [InlineData(100)]
    public void MovimentarEstoque_com_quantidade_positiva_nao_deve_ter_erros(double quantidade)
    {
        new MovimentarEstoqueRequestValidator().TestValidate(new MovimentarEstoqueRequest((decimal)quantidade))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MovimentarEstoque_com_quantidade_nao_positiva_deve_falhar(double quantidade)
    {
        new MovimentarEstoqueRequestValidator().TestValidate(new MovimentarEstoqueRequest((decimal)quantidade))
            .ShouldHaveValidationErrorFor(x => x.Quantidade);
    }
}
