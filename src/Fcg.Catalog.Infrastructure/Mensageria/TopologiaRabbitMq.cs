using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// Declara no RabbitMQ os exchanges, as filas e as DLQs que este serviço usa.
/// <para>
/// Toda declaração é idempotente: repetir com os mesmos argumentos não altera nada. Por isso
/// cada serviço declara o que usa ao iniciar, sem depender da ordem em que os serviços sobem.
/// Os argumentos precisam ser idênticos em todos os serviços — o RabbitMQ recusa redeclarar
/// uma fila existente com argumentos diferentes (PRECONDITION_FAILED).
/// </para>
/// </summary>
public sealed class TopologiaRabbitMq(IOptions<OpcoesDoRabbitMq> opcoes)
{
    private const string ArgumentoTipoDeFila = "x-queue-type";
    private const string FilaQuorum = "quorum";
    private const string ArgumentoLimiteDeEntregas = "x-delivery-limit";
    private const string ArgumentoExchangeDeMensagensMortas = "x-dead-letter-exchange";
    private const string ArgumentoRotaDeMensagensMortas = "x-dead-letter-routing-key";
    private const string SufixoDaDlq = ".dlq";

    /// <summary>
    /// Declara o exchange de eventos (topic: roteia pela routing key) e o de mensagens mortas
    /// (direct: entrega cada falha na DLQ da fila de origem).
    /// </summary>
    public async Task DeclararExchangesAsync(IChannel canal, CancellationToken cancellationToken)
    {
        await canal.ExchangeDeclareAsync(
            exchange: opcoes.Value.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await canal.ExchangeDeclareAsync(
            exchange: opcoes.Value.ExchangeDeMensagensMortas,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Declara a fila de um consumidor, liga ela à routing key do evento e cria a DLQ
    /// para onde vão as mensagens que esgotarem as tentativas.
    /// </summary>
    public async Task DeclararFilaAsync(
        IChannel canal,
        string fila,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await DeclararExchangesAsync(canal, cancellationToken);
        await DeclararFilaPrincipalAsync(canal, fila, routingKey, cancellationToken);
        await DeclararDlqAsync(canal, fila, cancellationToken);
    }

    /// <summary>
    /// Fila quorum com limite de entregas: depois de <see cref="LimitesDaMensageria.LimiteDeEntregas"/>
    /// reentregas o próprio broker move a mensagem para a DLQ, sem código de retry no serviço.
    /// </summary>
    private async Task DeclararFilaPrincipalAsync(
        IChannel canal,
        string fila,
        string routingKey,
        CancellationToken cancellationToken)
    {
        var argumentos = new Dictionary<string, object?>
        {
            [ArgumentoTipoDeFila] = FilaQuorum,
            [ArgumentoLimiteDeEntregas] = LimitesDaMensageria.LimiteDeEntregas,
            [ArgumentoExchangeDeMensagensMortas] = opcoes.Value.ExchangeDeMensagensMortas,
            [ArgumentoRotaDeMensagensMortas] = fila
        };

        await canal.QueueDeclareAsync(
            queue: fila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: argumentos,
            cancellationToken: cancellationToken);

        await canal.QueueBindAsync(
            queue: fila,
            exchange: opcoes.Value.Exchange,
            routingKey: routingKey,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// A DLQ guarda as mensagens que falharam de vez, para análise. Ela recebe pelo exchange de
    /// mensagens mortas usando o nome da fila de origem como routing key.
    /// </summary>
    private async Task DeclararDlqAsync(IChannel canal, string fila, CancellationToken cancellationToken)
    {
        var dlq = fila + SufixoDaDlq;
        var argumentos = new Dictionary<string, object?>
        {
            [ArgumentoTipoDeFila] = FilaQuorum
        };

        await canal.QueueDeclareAsync(
            queue: dlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: argumentos,
            cancellationToken: cancellationToken);

        await canal.QueueBindAsync(
            queue: dlq,
            exchange: opcoes.Value.ExchangeDeMensagensMortas,
            routingKey: fila,
            cancellationToken: cancellationToken);
    }
}
