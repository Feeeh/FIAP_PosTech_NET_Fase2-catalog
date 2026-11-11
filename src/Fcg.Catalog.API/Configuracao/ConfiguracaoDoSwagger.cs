using System.Reflection;
using Microsoft.OpenApi;

namespace Fcg.Catalog.API;

/// <summary>
/// Documentação OpenAPI. Além de listar os endpoints, declara o esquema de segurança,
/// para que o Swagger UI ganhe o botão "Authorize" e a API possa ser testada por ali
/// sem nenhuma ferramenta externa.
/// </summary>
public static class ConfiguracaoDoSwagger
{
    private const string EsquemaBearer = "Bearer";

    public static IServiceCollection AdicionarSwagger(this IServiceCollection servicos)
    {
        servicos.AddEndpointsApiExplorer();

        servicos.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FIAP Cloud Games (FCG) — API",
                Version = "v1",
                Description =
                    "Catálogo, promoções e compras assíncronas da FIAP Cloud Games."
            });

            opcoes.AddSecurityDefinition(EsquemaBearer, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Autentique no UsersAPI em POST /api/autenticacao/login e cole aqui apenas o token devolvido."
            });

            // Exige o Bearer por padrão em todos os endpoints documentados.
            // O Swashbuckle 10 recebe uma fábrica: o requisito é montado já com o
            // documento OpenAPI em mãos, para que a referência ao esquema resolva.
            opcoes.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(EsquemaBearer, documento), [] }
            });

            // Traz para o Swagger os comentários <summary> e <remarks> dos controllers.
            var arquivoDeComentarios = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var caminhoDosComentarios = Path.Combine(AppContext.BaseDirectory, arquivoDeComentarios);

            if (File.Exists(caminhoDosComentarios))
            {
                opcoes.IncludeXmlComments(caminhoDosComentarios);
            }
        });

        return servicos;
    }
}
