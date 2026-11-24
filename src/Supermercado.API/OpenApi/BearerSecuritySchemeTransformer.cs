using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Supermercado.API.OpenApi;

/// <summary>
/// Declara o esquema "Bearer" (JWT) no documento OpenAPI e o exige nas operações, exceto no login.
/// Com isso o Scalar mostra o campo de token e envia o cabeçalho Authorization nas chamadas.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string NomeDoEsquema = "Bearer";
    private const string CaminhoDoLogin = "/api/auth/login";

    private readonly IAuthenticationSchemeProvider _esquemas;

    public BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider esquemas)
    {
        _esquemas = esquemas;
    }

    public async Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var esquemas = await _esquemas.GetAllSchemesAsync();
        if (!esquemas.Any(e => e.Name == JwtBearerDefaults.AuthenticationScheme))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [NomeDoEsquema] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Cole o token retornado por POST /api/auth/login (sem o prefixo \"Bearer \").",
            }
        };

        foreach (var (caminho, item) in document.Paths)
        {
            if (caminho == CaminhoDoLogin || item.Operations is null)
                continue;

            foreach (var operacao in item.Operations.Values)
            {
                operacao.Security ??= [];
                operacao.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(NomeDoEsquema, document)] = []
                });
            }
        }
    }
}
