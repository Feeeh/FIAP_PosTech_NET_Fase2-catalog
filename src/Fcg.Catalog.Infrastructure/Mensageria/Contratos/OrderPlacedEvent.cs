using System.Text.Json.Serialization;

namespace Fcg.Catalog.Infrastructure.Mensageria.Contratos;

/// <summary>Pedido realizado pelo Catalog, consumido por Payments para decidir o pagamento.</summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredAt">Instante em que o pedido foi realizado.</param>
/// <param name="OrderId">Pedido que deve ser pago.</param>
/// <param name="UserId">Usuário que realizou a compra.</param>
/// <param name="UserEmail">E-mail informado pelo Catalog para a confirmação.</param>
/// <param name="GameId">Jogo comprado.</param>
/// <param name="GameTitle">Título congelado na compra.</param>
/// <param name="Price">Preço congelado na compra.</param>
public sealed record OrderPlacedEvent(
    [property: JsonRequired] Guid EventId,
    [property: JsonRequired] DateTimeOffset OccurredAt,
    [property: JsonRequired] Guid OrderId,
    [property: JsonRequired] Guid UserId,
    [property: JsonRequired] string UserEmail,
    [property: JsonRequired] Guid GameId,
    [property: JsonRequired] string GameTitle,
    [property: JsonRequired] decimal Price) : IEventoDeIntegracao;
