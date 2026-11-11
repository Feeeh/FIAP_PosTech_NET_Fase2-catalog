using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// Base de todo consumidor de eventos. Roda em segundo plano enquanto o serviço estiver de pé:
/// declara a fila, recebe cada mensagem e decide o destino dela.
/// <list type="bullet">
/// <item>Processou → <c>ack</c>: a mensagem sai da fila.</item>
/// <item>Contrato inválido → <c>reject</c> sem requeue: vai direto para a DLQ, porque repetir não conserta.</item>
/// <item>Outra falha → <c>nack</c> com requeue: o broker tenta de novo e, esgotado o limite, move para a DLQ.</item>
/// </list>
/// O consumidor concreto só informa a fila, a routing key, a validação e qual caso de uso chamar.
/// </summary>
public abstract class ConsumidorRabbitMq<TEvento>(
    ConexaoRabbitMq conexao,
    TopologiaRabbitMq topologia,
    IServiceScopeFactory escopos,
    ILogger logger) : BackgroundService where TEvento : class
{
    /// <summary>Fila de onde este consumidor lê.</summary>
    protected abstract string Fila { get; }

    /// <summary>Routing key do evento que deve chegar à fila.</summary>
    protected abstract string RoutingKey { get; }

    /// <summary>
    /// Confere os valores do contrato (ids preenchidos, preço não negativo...). Campos ausentes já
    /// falham antes, na desserialização, por causa do <c>[JsonRequired]</c> dos contratos.
    /// </summary>
    protected abstract bool EhValido(TEvento evento);

    /// <summary>Chama o caso de uso, resolvido nos serviços do escopo criado para esta mensagem.</summary>
    protected abstract Task ProcessarAsync(
        TEvento evento,
        IServiceProvider servicosDoEscopo,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uma exceção que escapa daqui encerra o host. É proposital: sem broker o serviço não
        // tem o que fazer, e o Docker/Kubernetes reinicia o contêiner.
        var ligacao = await conexao.ObterAsync(stoppingToken);
        await using var canal = await ligacao.CreateChannelAsync(cancellationToken: stoppingToken);

        await topologia.DeclararFilaAsync(canal, Fila, RoutingKey, stoppingToken);
        await canal.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: LimitesDaMensageria.Prefetch,
            global: false,
            cancellationToken: stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += (_, entrega) => ReceberAsync(canal, entrega, stoppingToken);

        await canal.BasicConsumeAsync(
            queue: Fila,
            autoAck: false,
            consumer: consumidor,
            cancellationToken: stoppingToken);

        // As mensagens chegam pelo evento acima; aqui só mantemos o canal aberto até o desligamento.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ReceberAsync(
        IChannel canal,
        BasicDeliverEventArgs entrega,
        CancellationToken cancellationToken)
    {
        // Um escopo por mensagem, como uma requisição HTTP: DbContext e correlação próprios,
        // sem estado compartilhado entre mensagens.
        await using var escopo = escopos.CreateAsyncScope();
        var correlacao = escopo.ServiceProvider.GetRequiredService<ContextoDeCorrelacao>();
        correlacao.Definir(entrega.BasicProperties.CorrelationId);

        // Com template, o log JSON mostra "CorrelationId abc" no escopo e o campo CorrelationId
        // separado, pronto para filtrar. Um Dictionary apareceria como o nome do tipo.
        using var escopoDeLog = logger.BeginScope("CorrelationId {CorrelationId}", correlacao.CorrelationId);

        try
        {
            var evento = JsonSerializer.Deserialize<TEvento>(entrega.Body.Span, JsonDaMensageria.Opcoes);

            if (evento is null || !EhValido(evento))
            {
                logger.LogWarning("Contrato inválido recebido em {Fila}; enviado para a DLQ.", Fila);
                await RejeitarAsync(canal, entrega, cancellationToken);
                return;
            }

            await ProcessarAsync(evento, escopo.ServiceProvider, cancellationToken);
            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (JsonException excecao)
        {
            logger.LogWarning(excecao, "JSON inválido recebido em {Fila}; enviado para a DLQ.", Fila);
            await RejeitarAsync(canal, entrega, cancellationToken);
        }
        catch (Exception excecao) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(excecao, "Falha ao processar mensagem de {Fila}; ela será reentregue.", Fila);

            // A pausa evita reentregas em sequência enquanto a dependência (banco, por exemplo) volta.
            await Task.Delay(LimitesDaMensageria.EsperaAntesDoNack, cancellationToken);
            await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    /// <summary>Sem requeue, a fila encaminha a mensagem para a DLQ configurada nela.</summary>
    private static async Task RejeitarAsync(
        IChannel canal,
        BasicDeliverEventArgs entrega,
        CancellationToken cancellationToken)
    {
        await canal.BasicRejectAsync(entrega.DeliveryTag, requeue: false, cancellationToken);
    }
}
