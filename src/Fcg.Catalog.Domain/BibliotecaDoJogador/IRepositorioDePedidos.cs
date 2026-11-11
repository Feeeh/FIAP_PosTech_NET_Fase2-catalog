namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>Carrega pedidos e protege a regra de uma compra pendente por jogo e usuário.</summary>
public interface IRepositorioDePedidos
{
    Task<Pedido?> ObterPorIdAsync(Guid pedidoId, CancellationToken cancellationToken = default);
    Task<bool> ExistePendenteAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default);
}
