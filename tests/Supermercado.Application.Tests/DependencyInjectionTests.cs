using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Services;
using Supermercado.Domain.Interfaces;

namespace Supermercado.Application.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider CriarProvider()
    {
        var services = new ServiceCollection();

        // Dependências que a Infrastructure/API fornecerão.
        services.AddSingleton(Substitute.For<IProdutoRepository>());
        services.AddSingleton(Substitute.For<ICategoriaRepository>());
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        services.AddSingleton(Substitute.For<IUsuarioAtual>());
        services.AddSingleton(Substitute.For<IUsuarioRepository>());
        services.AddSingleton(Substitute.For<IPasswordHasher>());
        services.AddSingleton(Substitute.For<ITokenService>());

        services.AddApplication();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void AddApplication_deve_registrar_os_services()
    {
        using var provider = CriarProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICategoriaService>().Should().BeOfType<CategoriaService>();
        scope.ServiceProvider.GetRequiredService<IProdutoService>().Should().BeOfType<ProdutoService>();
        scope.ServiceProvider.GetRequiredService<IAuthService>().Should().BeOfType<AuthService>();
        scope.ServiceProvider.GetRequiredService<IUsuarioService>().Should().BeOfType<UsuarioService>();
    }

    [Fact]
    public void AddApplication_deve_registrar_os_validators_de_cada_request()
    {
        using var provider = CriarProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IValidator<CriarCategoriaRequest>>().Should().NotBeNull();
        sp.GetRequiredService<IValidator<CriarProdutoRequest>>().Should().NotBeNull();
        sp.GetRequiredService<IValidator<AtualizarPrecoRequest>>().Should().NotBeNull();
        sp.GetRequiredService<IValidator<MovimentarEstoqueRequest>>().Should().NotBeNull();
        sp.GetRequiredService<IValidator<LoginRequest>>().Should().NotBeNull();
        sp.GetRequiredService<IValidator<CriarUsuarioRequest>>().Should().NotBeNull();
    }
}
