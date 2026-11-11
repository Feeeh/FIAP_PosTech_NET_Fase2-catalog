namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// O que todo contrato trocado entre os serviços tem em comum: um identificador único
/// e o instante em que o fato aconteceu.
/// <para>
/// A outbox usa o <see cref="EventId"/> como chave da linha e como MessageId no RabbitMQ.
/// É isso que permite reconhecer a mesma mensagem quando ela é entregue duas vezes.
/// </para>
/// </summary>
public interface IEventoDeIntegracao
{
    /// <summary>Identificador único do evento.</summary>
    Guid EventId { get; }

    /// <summary>Instante em que o fato aconteceu no serviço que publicou o evento.</summary>
    DateTimeOffset OccurredAt { get; }
}
