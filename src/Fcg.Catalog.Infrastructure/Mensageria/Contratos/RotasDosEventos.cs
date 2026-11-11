namespace Fcg.Catalog.Infrastructure.Mensageria.Contratos;

/// <summary>Rotas usadas pelos quatro serviços para ligar cada evento às filas de seus interessados.</summary>
public static class RotasDosEventos
{
    /// <summary>Users publica o cadastro para Notifications enviar boas-vindas.</summary>
    public const string UsuarioCriado = "user.created";

    /// <summary>Catalog publica a compra para Payments processar o pagamento.</summary>
    public const string PedidoRealizado = "order.placed";

    /// <summary>Payments publica o resultado para Catalog e Notifications.</summary>
    public const string PagamentoProcessado = "payment.processed";
}
