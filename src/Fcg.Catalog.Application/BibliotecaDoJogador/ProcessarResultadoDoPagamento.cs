using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Microsoft.Extensions.Logging;

namespace Fcg.Catalog.Application.BibliotecaDoJogador;

/// <summary>Comando com a decisão do pagamento para concluir a compra sem depender do transporte.</summary>
public sealed record ProcessarResultadoDoPagamentoComando(
    Guid PedidoId,
    bool Aprovado,
    string? Motivo);

/// <summary>Conclui a compra e concede acesso numa única transação, tolerando reentregas.</summary>
public sealed class ProcessarResultadoDoPagamento(
    IRepositorioDePedidos pedidos,
    IRepositorioDeBibliotecas bibliotecas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<ProcessarResultadoDoPagamento> logger)
{
    public async Task ExecutarAsync(
        ProcessarResultadoDoPagamentoComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var pedido = await pedidos.ObterPorIdAsync(comando.PedidoId, cancellationToken);

        if (pedido is null)
        {
            logger.LogWarning("Pagamento recebido para pedido desconhecido {PedidoId}.", comando.PedidoId);
            return;
        }

        if (pedido.Status != StatusPedido.Pendente)
        {
            return;
        }

        await AplicarResultadoAsync(pedido, comando, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        logger.LogInformation("Pedido {PedidoId} concluído: {Status}.", pedido.Id, pedido.Status);
    }

    private async Task AplicarResultadoAsync(
        Pedido pedido, ProcessarResultadoDoPagamentoComando comando, CancellationToken cancellationToken)
    {
        var instante = relogio.GetUtcNow();

        if (comando.Aprovado)
        {
            pedido.Aprovar(instante);
            await ConcederAcessoAsync(pedido, instante, cancellationToken);
        }
        else
        {
            pedido.Rejeitar(comando.Motivo ?? string.Empty, instante);
        }
    }

    private async Task ConcederAcessoAsync(
        Pedido pedido, DateTimeOffset instante, CancellationToken cancellationToken)
    {
        var biblioteca = await bibliotecas.ObterPorUsuarioAsync(pedido.UsuarioId, cancellationToken);
        var bibliotecaEhNova = biblioteca is null;
        biblioteca ??= Biblioteca.Criar(pedido.UsuarioId);

        if (biblioteca.Possui(pedido.JogoId))
        {
            logger.LogWarning("Pedido {PedidoId} aprovado para jogo já adquirido.", pedido.Id);
            return;
        }

        biblioteca.Adquirir(
            pedido.JogoId,
            pedido.TituloDoJogo,
            pedido.Valor,
            instante);

        if (bibliotecaEhNova)
        {
            await bibliotecas.AdicionarAsync(biblioteca, cancellationToken);
        }
    }
}
