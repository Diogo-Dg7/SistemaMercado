using System.Text.Json.Serialization;
using Scalar.AspNetCore;
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

// Enums trafegam como texto ("Quilograma"), igual ao que fica gravado no banco.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// CORS só em Development: libera o front (Vite) em http://localhost:5173.
const string PoliticaCorsFrontend = "Frontend";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(opcoes => opcoes.AddPolicy(PoliticaCorsFrontend, politica => politica
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

// Erros viram ProblemDetails (RFC 9457) pelo GlobalExceptionHandler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // respostas sem corpo (404 de rota, 405, 415...) também saem como ProblemDetails
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(PoliticaCorsFrontend);
    app.MapOpenApi();              // /openapi/v1.json
    app.MapScalarApiReference();   // /scalar
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }
