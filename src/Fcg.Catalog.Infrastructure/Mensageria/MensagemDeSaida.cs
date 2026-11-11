using System.Text.Json;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Evento salvo junto do agregado; o dispatcher o envia ao RabbitMQ e registra a confirmação.</summary>
public sealed class MensagemDeSaida
{
    private MensagemDeSaida()
    {
    }

    /// <summary>EventId do contrato; também será o MessageId no RabbitMQ.</summary>
    public Guid Id { get; private set; }

    /// <summary>Nome do contrato usado na propriedade AMQP Type.</summary>
    public string Tipo { get; private set; } = string.Empty;

    /// <summary>Rota que seleciona as filas interessadas no evento.</summary>
    public string RoutingKey { get; private set; } = string.Empty;

    /// <summary>JSON pronto para envio, preservado durante as tentativas.</summary>
    public string Conteudo { get; private set; } = string.Empty;

    /// <summary>Correlação recebida pela API ou consumidor para acompanhar a mesma operação.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Instante do evento, usado para ordenar as pendências.</summary>
    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>Instante da confirmação pelo broker; nulo enquanto o envio estiver pendente.</summary>
    public DateTimeOffset? EnviadaEm { get; private set; }

    /// <summary>
    /// Serializa o contrato no formato de fio e usa o EventId dele como identidade da linha
    /// e da mensagem AMQP.
    /// </summary>
    public static MensagemDeSaida Criar<TContrato>(
        TContrato contrato,
        string routingKey,
        string correlationId) where TContrato : IEventoDeIntegracao
    {
        return new MensagemDeSaida
        {
            Id = contrato.EventId,
            Tipo = typeof(TContrato).Name,
            RoutingKey = routingKey,
            Conteudo = JsonSerializer.Serialize(contrato, JsonDaMensageria.Opcoes),
            CorrelationId = correlationId,
            CriadaEm = contrato.OccurredAt
        };
    }

    /// <summary>Chamado pelo dispatcher somente depois da confirmação de publicação.</summary>
    public void MarcarComoEnviada(DateTimeOffset instante)
    {
        EnviadaEm = instante;
    }
}
