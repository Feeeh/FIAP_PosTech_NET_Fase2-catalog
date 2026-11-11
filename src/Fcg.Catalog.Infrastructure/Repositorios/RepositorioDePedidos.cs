using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Infrastructure.Repositorios;

/// <summary>Persiste o pedido no mesmo contexto que a biblioteca e a outbox.</summary>
public sealed class RepositorioDePedidos(CatalogoDbContext contexto) : IRepositorioDePedidos
{
    public Task<Pedido?> ObterPorIdAsync(Guid pedidoId, CancellationToken cancellationToken = default) =>
        contexto.Pedidos.FirstOrDefaultAsync(pedido => pedido.Id == pedidoId, cancellationToken);

    public Task<bool> ExistePendenteAsync(
        Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default) =>
        contexto.Pedidos.AnyAsync(
            pedido => pedido.UsuarioId == usuarioId
                && pedido.JogoId == jogoId
                && pedido.Status == StatusPedido.Pendente,
            cancellationToken);

    public async Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default) =>
        await contexto.Pedidos.AddAsync(pedido, cancellationToken);
}
