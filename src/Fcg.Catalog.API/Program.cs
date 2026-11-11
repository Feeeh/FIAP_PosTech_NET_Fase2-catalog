using System.Text.Json.Serialization;
using Fcg.Catalog.API;
using Fcg.Catalog.API.Middlewares;
using Fcg.Catalog.Infrastructure;
using Fcg.Catalog.Infrastructure.Persistencia;
using Fcg.Catalog.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var construtor = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logs estruturados. O formatador JSON nativo já entrega campos
// consultáveis; as mensagens sempre usam template, nunca concatenação.
// ---------------------------------------------------------------------------
construtor.Logging.ClearProviders();
construtor.Logging.AddJsonConsole(opcoes => opcoes.IncludeScopes = true);

// ---------------------------------------------------------------------------
// Configuração
// ---------------------------------------------------------------------------
construtor.Services.Configure<OpcoesDeJwt>(construtor.Configuration.GetSection(OpcoesDeJwt.Secao));

var opcoesDeJwt = construtor.Configuration.GetSection(OpcoesDeJwt.Secao).Get<OpcoesDeJwt>() ?? new OpcoesDeJwt();

// Falha na inicialização, e não na primeira autenticação, se a chave não foi configurada.
opcoesDeJwt.Validar();

// ---------------------------------------------------------------------------
// Serviços
// ---------------------------------------------------------------------------
construtor.Services
    .AdicionarInfraestrutura(construtor.Configuration)
    .AdicionarCasosDeUso()
    .AdicionarSwagger();

construtor.Services
    .AddControllers(opcoes =>
    {
        // Por padrão o ASP.NET remove o sufixo "Async" do nome da action, e aí
        // CreatedAtAction(nameof(MinhaActionAsync)) não acha a rota e quebra em runtime.
        // Mantendo o sufixo, o nameof continua valendo e sobrevive a um rename.
        opcoes.SuppressAsyncSuffixInActionNames = false;
    })
    .AddJsonOptions(opcoes => opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

construtor.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // Todas as validações ligadas: um token sem emissor, audiência, assinatura
        // ou prazo conferidos não é uma credencial confiável.
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opcoesDeJwt.Emissor,
            ValidAudience = opcoesDeJwt.Audiencia,
            IssuerSigningKey = opcoesDeJwt.ObterChaveDeAssinatura(),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

construtor.Services.AddAuthorization();

var aplicacao = construtor.Build();

// ---------------------------------------------------------------------------
// Pipeline. A ordem importa: o CorrelationId precisa existir antes de qualquer
// log, e o tratamento de erros precisa envolver tudo que vem depois dele.
// ---------------------------------------------------------------------------
aplicacao.UseMiddleware<CorrelationIdMiddleware>();
aplicacao.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger fica disponível nos contêineres quando Swagger:Habilitado = true (ConfigMap).
var swagger = construtor.Configuration.GetSection(OpcoesDoSwagger.Secao).Get<OpcoesDoSwagger>();

if (aplicacao.Environment.IsDevelopment() || swagger?.Habilitado == true)
{
    aplicacao.UseSwagger();
    aplicacao.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG API v1");
        opcoes.DocumentTitle = "FIAP Cloud Games — API";
    });
}

// Sem UseHttpsRedirection: dentro do cluster o tráfego é HTTP. TLS pertence a um Ingress,
// fora do escopo desta fase — redirecionar aqui quebraria as chamadas internas.
aplicacao.UseAuthentication();
aplicacao.UseAuthorization();

aplicacao.MapControllers();

// Endpoint simples para o docker compose saber quando a API subiu.
aplicacao.MapGet("/saude", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await aplicacao.Services.MigrarAsync<CatalogoDbContext>();

await aplicacao.RunAsync();

/// <summary>
/// Exposto para que os testes de integração possam instanciar a aplicação
/// com <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
