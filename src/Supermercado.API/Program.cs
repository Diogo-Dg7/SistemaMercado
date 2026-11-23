using Supermercado.API.Services;
using Supermercado.Application;
using Supermercado.Application.Interfaces;
using Supermercado.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// TODO: trocar pela implementação baseada em JWT quando a autenticação for criada.
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualProvisorio>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }
