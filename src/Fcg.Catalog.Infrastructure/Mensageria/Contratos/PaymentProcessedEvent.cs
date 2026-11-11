using System.Text.Json.Serialization;

namespace Fcg.Catalog.Infrastructure.Mensageria.Contratos;

/// <summary>
/// Resultado publicado por Payments; Catalog atualiza o pedido e Notifications confirma compras aprovadas.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredAt">Instante do processamento.</param>
/// <param name="PaymentId">Pagamento registrado por Payments.</param>
/// <param name="OrderId">Pedido cujo resultado será atualizado.</param>
/// <param name="UserId">Usuário que realizou a compra.</param>
/// <param name="UserEmail">E-mail para enviar a confirmação.</param>
/// <param name="GameId">Jogo comprado.</param>
/// <param name="GameTitle">Título congelado na compra.</param>
/// <param name="Price">Preço usado na decisão.</param>
/// <param name="Status">Resultado Approved ou Rejected.</param>
/// <param name="Reason">Motivo da rejeição; nulo quando aprovado.</param>
public sealed record PaymentProcessedEvent(
    [property: JsonRequired] Guid EventId,
    [property: JsonRequired] DateTimeOffset OccurredAt,
    [property: JsonRequired] Guid PaymentId,
    [property: JsonRequired] Guid OrderId,
    [property: JsonRequired] Guid UserId,
    [property: JsonRequired] string UserEmail,
    [property: JsonRequired] Guid GameId,
    [property: JsonRequired] string GameTitle,
    [property: JsonRequired] decimal Price,
    [property: JsonRequired] string Status,
    [property: JsonRequired] string? Reason) : IEventoDeIntegracao
{
    /// <summary>Valor do contrato que informa uma aprovação aos demais serviços.</summary>
    public const string Approved = "Approved";

    /// <summary>Valor do contrato que informa uma rejeição aos demais serviços.</summary>
    public const string Rejected = "Rejected";
}
