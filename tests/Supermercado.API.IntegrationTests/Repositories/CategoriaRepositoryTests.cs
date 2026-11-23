using Microsoft.Extensions.DependencyInjection;
using Supermercado.API.IntegrationTests.Infrastructure;
using Supermercado.Domain.Interfaces;

namespace Supermercado.API.IntegrationTests.Repositories;

[Collection(BancoCollection.Nome)]
public class CategoriaRepositoryTests : IntegracaoTestBase
{
    public CategoriaRepositoryTests(BancoDeDadosFixture banco) : base(banco)
    {
    }

    [Fact]
    public async Task Salvar_e_buscar_por_id_deve_preservar_os_campos()
    {
        var original = await SalvarCategoriaAsync("Limpeza");

        using var escopo = Banco.NovoEscopo();
        var lida = await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>().ObterPorIdAsync(original.Id);

        lida.Should().NotBeNull();
        lida!.Id.Should().Be(original.Id);
        lida.Nome.Should().Be("Limpeza");
        lida.CriadoEm.Should().BeCloseTo(original.CriadoEm, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Buscar_por_id_inexistente_deve_retornar_nulo()
    {
        using var escopo = Banco.NovoEscopo();

        (await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>().ObterPorIdAsync(Guid.NewGuid()))
            .Should().BeNull();
    }

    [Fact]
    public async Task Listar_deve_retornar_todas_ordenadas_por_nome()
    {
        await SalvarCategoriaAsync("Limpeza");
        await SalvarCategoriaAsync("Bebidas");
        await SalvarCategoriaAsync("Padaria");

        using var escopo = Banco.NovoEscopo();
        var categorias = await escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>().ListarAsync();

        categorias.Select(c => c.Nome).Should().Equal("Bebidas", "Limpeza", "Padaria");
    }

    [Fact]
    public async Task Alterar_deve_persistir_o_novo_nome()
    {
        var original = await SalvarCategoriaAsync("Bebidas");

        using (var escopo = Banco.NovoEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<ICategoriaRepository>();
            var categoria = (await repositorio.ObterParaAlteracaoAsync(original.Id))!;
            categoria.Renomear("Bebidas e sucos");
            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SalvarAlteracoesAsync();
        }

        using var leitura = Banco.NovoEscopo();
        (await leitura.ServiceProvider.GetRequiredService<ICategoriaRepository>().ObterPorIdAsync(original.Id))!
            .Nome.Should().Be("Bebidas e sucos");
    }
}
