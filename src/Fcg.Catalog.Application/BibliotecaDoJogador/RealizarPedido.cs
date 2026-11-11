using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.SeedWork;
using Microsoft.Extensions.Logging;

namespace Fcg.Catalog.Application.BibliotecaDoJogador;

/// <summary>Dados da solicitação cuja identidade vem do token validado.</summary>
public sealed record RealizarPedidoComando(Guid UsuarioId, string UsuarioEmail, Guid JogoId);

/// <summary>Recurso consultável enquanto a compra aguarda o pagamento.</summary>
public sealed record PedidoResposta(
    Guid PedidoId,
    Guid JogoId,
    string Titulo,
    decimal Valor,
    StatusPedido Status,
    string? Motivo,
    DateTimeOffset CriadoEm,
    DateTimeOffset? ConcluidoEm)
{
    public static PedidoResposta De(Pedido pedido) => new(
        pedido.Id,
        pedido.JogoId,
        pedido.TituloDoJogo,
        pedido.Valor.Valor,
        pedido.Status,
        pedido.Motivo,
        pedido.CriadoEm,
        pedido.ConcluidoEm);
}

/// <summary>Caso de uso que carrega os agregados e salva o pedido para solicitar o pagamento.</summary>
public sealed class RealizarPedido(
    IRepositorioDeBibliotecas bibliotecas,
    IRepositorioDeJogos jogos,
    IRepositorioDePromocoes promocoes,
    IRepositorioDePedidos pedidos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<RealizarPedido> logger)
{
    public async Task<PedidoResposta> ExecutarAsync(
        RealizarPedidoComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var instante = relogio.GetUtcNow();
        var jogo = await jogos.ObterPorIdAsync(comando.JogoId, cancellationToken)
            ?? throw new ExcecaoDeNaoEncontrado("Jogo não encontrado.");

        // A promoção é buscada no instante da compra, não no da consulta ao catálogo.
        var biblioteca = await bibliotecas.ObterPorUsuarioAsync(comando.UsuarioId, cancellationToken);
        var promocao = await promocoes.ObterVigenteParaJogoAsync(jogo.Id, instante, cancellationToken);

        // Precisa de banco, por isso fica no caso de uso; o índice único filtrado cobre a corrida.
        if (await pedidos.ExistePendenteAsync(comando.UsuarioId, comando.JogoId, cancellationToken))
        {
            throw new ExcecaoDeConflito("Já existe um pedido pendente para este jogo.");
        }

        var pedido = ServicoDeAquisicao.RealizarPedido(
            biblioteca,
            jogo,
            promocao,
            comando.UsuarioId,
            comando.UsuarioEmail,
            instante);

        await pedidos.AdicionarAsync(pedido, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        logger.LogInformation(
            "Pedido {PedidoId} realizado para o jogo {JogoId}; aguardando o pagamento.",
            pedido.Id,
            pedido.JogoId);

        return PedidoResposta.De(pedido);
    }
}
