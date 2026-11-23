using Supermercado.API.ExceptionHandlers;
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

// Erros viram ProblemDetails (RFC 9457) pelo GlobalExceptionHandler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // respostas sem corpo (404 de rota, 405, 415...) também saem como ProblemDetails
app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }
