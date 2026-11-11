using System.Net.Mime;
using Fcg.Catalog.Domain.SeedWork;
using Fcg.Catalog.Infrastructure.Mensageria;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Catalog.API.Middlewares;

/// <summary>
/// Tratamento global de erros. Traduz cada exceção de domínio no status HTTP
/// correspondente e devolve sempre o mesmo formato — ProblemDetails (RFC 7807).
/// <para>
/// Concentrar isso aqui é o que permite que nenhum controller precise de try/catch:
/// a preocupação transversal fica em um lugar só.
/// </para>
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate proximo, ILogger<ErrorHandlingMiddleware> logger)
{
    private const string MensagemGenericaDeErroInterno =
        "Ocorreu um erro inesperado ao processar a requisição.";

    public async Task InvokeAsync(HttpContext contexto, ContextoDeCorrelacao contextoDeCorrelacao)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        try
        {
            await proximo(contexto);
        }
        catch (ExcecaoDeDominio excecaoDeNegocio)
        {
            // Erro previsto: é uma regra de negócio funcionando, não uma falha do sistema.
            logger.LogInformation(
                "Requisição rejeitada por regra de negócio: {TipoDaRegra} - {MotivoDaRejeicao}",
                excecaoDeNegocio.GetType().Name,
                excecaoDeNegocio.Message);

            await EscreverProblemaAsync(
                contexto,
                contextoDeCorrelacao,
                TraduzirParaStatusHttp(excecaoDeNegocio),
                TituloPara(excecaoDeNegocio),
                excecaoDeNegocio.Message);
        }
        catch (Exception excecaoInesperada)
        {
            // Falha não prevista: registra tudo no log, mas não devolve detalhe nenhum
            // ao cliente — stack trace e mensagem interna podem revelar a estrutura do sistema.
            logger.LogError(excecaoInesperada, "Falha não tratada ao processar a requisição.");

            await EscreverProblemaAsync(
                contexto,
                contextoDeCorrelacao,
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                MensagemGenericaDeErroInterno);
        }
    }

    private static int TraduzirParaStatusHttp(ExcecaoDeDominio excecao) => excecao switch
    {
        ExcecaoDeValidacao => StatusCodes.Status400BadRequest,
        ExcecaoDeAutenticacao => StatusCodes.Status401Unauthorized,
        ExcecaoDeNaoEncontrado => StatusCodes.Status404NotFound,
        ExcecaoDeConflito => StatusCodes.Status409Conflict,
        ExcecaoDeEstadoInvalido => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest
    };

    private static string TituloPara(ExcecaoDeDominio excecao) => excecao switch
    {
        ExcecaoDeValidacao => "Dados inválidos",
        ExcecaoDeAutenticacao => "Não autenticado",
        ExcecaoDeNaoEncontrado => "Recurso não encontrado",
        ExcecaoDeConflito => "Conflito com o estado atual",
        ExcecaoDeEstadoInvalido => "Operação não permitida no estado atual",
        _ => "Requisição inválida"
    };

    private static async Task EscreverProblemaAsync(
        HttpContext contexto,
        ContextoDeCorrelacao contextoDeCorrelacao,
        int statusHttp,
        string titulo,
        string detalhe)
    {
        if (contexto.Response.HasStarted)
        {
            // A resposta já começou a ser enviada; não dá para trocar o status agora.
            return;
        }

        var problema = new ProblemDetails
        {
            Status = statusHttp,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path
        };

        // Permite ao usuário citar o identificador da requisição ao relatar o erro.
        problema.Extensions["correlationId"] = contextoDeCorrelacao.CorrelationId;

        contexto.Response.Clear();
        contexto.Response.StatusCode = statusHttp;

        // O content type vai como argumento porque WriteAsJsonAsync sobrescreve o que
        // estiver em Response.ContentType — sem isso a resposta sairia como application/json.
        await contexto.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: MediaTypeNames.Application.ProblemJson);
    }
}

