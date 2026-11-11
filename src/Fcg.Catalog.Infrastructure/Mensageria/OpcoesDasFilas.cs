namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Define a fila exclusiva do Catalog para concluir pedidos.</summary>
public sealed class OpcoesDasFilas
{
    public const string Secao = "RabbitMq:Filas";
    public string PaymentProcessed { get; set; } = "catalog.payment-processed";
}
