using Fcg.Catalog.Infrastructure.Mensageria;

namespace Fcg.Catalog.API.Middlewares;

/// <summary>
/// Dá a cada requisição um identificador único e o coloca no escopo do log.
/// <para>
/// É o primeiro middleware do pipeline de propósito: tudo que for registrado depois
/// dele — inclusive os erros — carrega o mesmo CorrelationId, o que permite reconstruir
/// uma requisição inteira a partir de uma única busca nos logs.
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate proximo, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Cabeçalho lido do cliente e devolvido na resposta.</summary>
    public const string NomeDoCabecalho = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext contexto, ContextoDeCorrelacao contextoDeCorrelacao)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(contextoDeCorrelacao);

        // O cabeçalho vem do cliente: o contexto só o adota se for seguro, senão gera um novo.
        var recebido = contexto.Request.Headers[NomeDoCabecalho].FirstOrDefault();
        var correlationId = contextoDeCorrelacao.Definir(recebido);

        // Devolve o identificador para que o cliente possa citá-lo ao relatar um problema.
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[NomeDoCabecalho] = correlationId;
            return Task.CompletedTask;
        });

        // Tudo logado dentro deste escopo sai com o CorrelationId anexado.
        using (logger.BeginScope("CorrelationId {CorrelationId}", correlationId))
        {
            await proximo(contexto);
        }
    }
}
