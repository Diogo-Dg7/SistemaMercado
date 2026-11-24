using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Supermercado.API.Authorization;
using Supermercado.API.ExceptionHandlers;
using Supermercado.API.OpenApi;
using Supermercado.API.Seeding;
using Supermercado.API.Services;
using Supermercado.Application;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Security;
using Supermercado.Domain.Enums;
using Supermercado.Infrastructure;
using Supermercado.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Perfil do usuário da requisição, lido das claims do JWT.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

// Autenticação JWT. A chave vem de Jwt:Chave (user-secrets); a Infrastructure já recusa a inicialização
// da API se ela estiver ausente ou com menos de 32 caracteres.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearer.MapInboundClaims = false; // mantém "sub", "name", "email" e "role" como foram emitidas
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = NomesDeClaim.Nome,
            RoleClaimType = NomesDeClaim.Perfil,
        };
    });

// Autorização por perfil. Por padrão TODO endpoint exige usuário autenticado (política de fallback);
// /health e a documentação em Development são liberados explicitamente com AllowAnonymous.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.ApenasGerente, p => p.RequireRole(nameof(PerfilUsuario.Gerente)))
    .AddPolicy(Politicas.GerenteOuOperador, p => p.RequireRole(nameof(PerfilUsuario.Gerente), nameof(PerfilUsuario.Operador)))
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Cria o primeiro Gerente na inicialização, só se ainda não existir nenhum usuário.
builder.Services.AddHostedService<AdministradorInicialSeeder>();

// Enums trafegam como texto ("Quilograma"), igual ao que fica gravado no banco.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(opcoes => opcoes.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

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
builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
{
    // Respostas geradas pelo próprio framework (401/403 da autorização, 404 de rota...) saem com título em português,
    // como as do GlobalExceptionHandler. Títulos já definidos por ele não são alterados.
    if (contexto.Exception is null && TitulosPadrao.TryGetValue(contexto.ProblemDetails.Status ?? 0, out var titulo))
        contexto.ProblemDetails.Title = titulo;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // respostas sem corpo (404 de rota, 405, 415...) também saem como ProblemDetails
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
    app.UseCors(PoliticaCorsFrontend);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();   // /openapi/v1.json
    app.MapScalarApiReference(opcoes => opcoes
            .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.NomeDoEsquema))
        .AllowAnonymous();               // /scalar
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program
{
    private static readonly Dictionary<int, string> TitulosPadrao = new()
    {
        [StatusCodes.Status401Unauthorized] = "Não autorizado",
        [StatusCodes.Status403Forbidden] = "Acesso negado",
        [StatusCodes.Status404NotFound] = "Recurso não encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método não permitido",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de mídia não suportado",
    };
}
