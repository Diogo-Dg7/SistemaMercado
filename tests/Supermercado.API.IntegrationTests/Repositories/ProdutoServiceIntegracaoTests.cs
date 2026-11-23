using Microsoft.Extensions.DependencyInjection;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;

namespace Supermercado.API.IntegrationTests.Repositories;

/// <summary>Casos de uso reais (Application) sobre repositórios reais (Infrastructure) e SQL Server.</summary>
[Collection(BancoCollection.Nome)]
public class ProdutoServiceIntegracaoTests : IntegracaoTestBase
{
    public ProdutoServiceIntegracaoTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    private async Task<ProdutoDto> CriarProdutoViaServiceAsync(
        Guid categoriaId, string? codigo = null, decimal estoque = 20m, decimal minimo = 10m,
        UnidadeMedida unidade = UnidadeMedida.Unidade)
    {
        using var escopo = Banco.NovoEscopo();
        return await escopo.ServiceProvider.GetRequiredService<IProdutoService>().CriarAsync(new CriarProdutoRequest(
            "Refrigerante 2L", codigo ?? GeradorCodigoBarras.Proximo(), 5m, 8m, categoriaId, unidade, minimo, estoque));
    }

    [Fact]
    public async Task Criar_e_obter_pelo_service_deve_persistir_no_banco()
    {
        var categoria = await SalvarCategoriaAsync();

        var criado = await CriarProdutoViaServiceAsync(categoria.Id, estoque: 20m);

        using var escopo = Banco.NovoEscopo();
        var obtido = await escopo.ServiceProvider.GetRequiredService<IProdutoService>().ObterPorIdAsync(criado.Id);
        obtido.Should().Be(criado);
        obtido.Estoque.Should().Be(20m);
    }

    [Fact]
    public async Task Criar_com_codigo_de_barras_ja_cadastrado_deve_lancar_DomainException()
    {
        var categoria = await SalvarCategoriaAsync();
        var codigo = GeradorCodigoBarras.Proximo();
        await CriarProdutoViaServiceAsync(categoria.Id, codigo);

        var act = () => CriarProdutoViaServiceAsync(categoria.Id, codigo);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*código de barras*");
    }

    [Fact]
    public async Task Atualizar_preco_abaixo_do_custo_deve_depender_do_perfil()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await CriarProdutoViaServiceAsync(categoria.Id);

        using (var escopoOperador = Banco.NovoEscopo(PerfilUsuario.Operador))
        {
            var act = () => escopoOperador.ServiceProvider.GetRequiredService<IProdutoService>()
                .AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(10m, 9m));

            await act.Should().ThrowAsync<DomainException>();
        }

        using (var escopoGerente = Banco.NovoEscopo(PerfilUsuario.Gerente))
        {
            await escopoGerente.ServiceProvider.GetRequiredService<IProdutoService>()
                .AtualizarPrecoAsync(produto.Id, new AtualizarPrecoRequest(10m, 9m));
        }

        using var leitura = Banco.NovoEscopo();
        var final = await leitura.ServiceProvider.GetRequiredService<IProdutoService>().ObterPorIdAsync(produto.Id);
        final.PrecoCusto.Should().Be(10m);
        final.PrecoVenda.Should().Be(9m);
    }

    [Fact]
    public async Task Movimentar_estoque_deve_persistir_e_alimentar_o_alerta_de_reposicao()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await CriarProdutoViaServiceAsync(
            categoria.Id, estoque: 10m, minimo: 5m, unidade: UnidadeMedida.Quilograma);

        using (var escopo = Banco.NovoEscopo())
        {
            var service = escopo.ServiceProvider.GetRequiredService<IProdutoService>();
            await service.AdicionarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(0.5m));
            await service.BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(5.625m));
        }

        using var leitura = Banco.NovoEscopo();
        var leituraService = leitura.ServiceProvider.GetRequiredService<IProdutoService>();
        (await leituraService.ObterPorIdAsync(produto.Id)).Estoque.Should().Be(4.875m);

        var emFalta = await leituraService.ListarAbaixoDoEstoqueMinimoAsync();
        emFalta.Should().ContainSingle().Which.Id.Should().Be(produto.Id);
    }

    [Fact]
    public async Task Baixar_estoque_acima_do_disponivel_deve_lancar_excecao_e_manter_o_saldo()
    {
        var categoria = await SalvarCategoriaAsync();
        var produto = await CriarProdutoViaServiceAsync(categoria.Id, estoque: 3m);

        using (var escopo = Banco.NovoEscopo())
        {
            var act = () => escopo.ServiceProvider.GetRequiredService<IProdutoService>()
                .BaixarEstoqueAsync(produto.Id, new MovimentarEstoqueRequest(4m));

            await act.Should().ThrowAsync<EstoqueInsuficienteException>();
        }

        using var leitura = Banco.NovoEscopo();
        (await leitura.ServiceProvider.GetRequiredService<IProdutoService>().ObterPorIdAsync(produto.Id))
            .Estoque.Should().Be(3m);
    }
}
