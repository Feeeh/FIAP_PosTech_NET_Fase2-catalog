using Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;
using Fcg.Catalog.Domain.SeedWork;
using Fcg.Catalog.Infrastructure.Mensageria.Contratos;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Publica somente a solicitação de pagamento; demais fatos permanecem internos.</summary>
public static class MapeadorDeEventosDeIntegracao
{
    public static MensagemDeSaida? Mapear(EventoDeDominio evento, string correlationId)
    {
        if (evento is not PedidoRealizado pedido)
        {
            return null;
        }

        var contrato = new OrderPlacedEvent(
            Guid.NewGuid(),
            pedido.OcorridoEm,
            pedido.PedidoId,
            pedido.UsuarioId,
            pedido.UsuarioEmail,
            pedido.JogoId,
            pedido.TituloDoJogo,
            pedido.Valor);

        return MensagemDeSaida.Criar(contrato, RotasDosEventos.PedidoRealizado, correlationId);
    }
}
