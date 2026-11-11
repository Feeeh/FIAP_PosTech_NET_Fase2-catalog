using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;

/// <summary>Fato que permite publicar a compra sem acoplar o domínio ao transporte.</summary>
public sealed record PedidoRealizado(
    Guid PedidoId,
    Guid UsuarioId,
    string UsuarioEmail,
    Guid JogoId,
    string TituloDoJogo,
    decimal Valor)
    : EventoDeDominio;
