using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace euSindico.Api.OpenApi;

/// <summary>
/// Adiciona o botão "Authorize" ao documento OpenAPI (visível no Scalar) para os endpoints
/// protegidos por JWT Bearer — sem isso, o Scalar não sabe como anexar o token nas
/// requisições de "Try it" e cada chamada exigiria colar o header manualmente.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var esquemasDeAutenticacao = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!esquemasDeAutenticacao.Any(esquema => esquema.Name == "Bearer"))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
            Description = "Cole aqui o accessToken retornado por POST /auth/login (sem o prefixo \"Bearer \").",
        };

        var referenciaBearer = new OpenApiSecuritySchemeReference("Bearer", document);

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations!.Values))
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement { [referenciaBearer] = [] });
        }
    }
}
