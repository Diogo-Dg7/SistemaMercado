using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Services;
using Supermercado.Application.Validators;

namespace Supermercado.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProdutoService, ProdutoService>();

        services.AddValidatorsFromAssemblyContaining<CriarCategoriaRequestValidator>();

        return services;
    }
}
