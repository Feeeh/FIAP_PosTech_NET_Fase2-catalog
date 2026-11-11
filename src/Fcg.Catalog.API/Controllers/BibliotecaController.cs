using Fcg.Catalog.Application.BibliotecaDoJogador;
using Fcg.Catalog.Application.Consultas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Catalog.API.Controllers;

/// <summary>Solicitação de compra que identifica o jogo; o dono vem do token validado.</summary>
public sealed record AdquirirJogoRequisicao(Guid JogoId);

/// <summary>
/// Biblioteca do jogador, que permite consultar aquisições e solicitar novas compras.
/// <para>
/// Todas as operações agem sobre a biblioteca do usuário do token. Não existe rota
/// para acessar a biblioteca de outra pessoa.
/// </para>
/// </summary>
[ApiController]
[Route("api/biblioteca")]
[Produces("application/json")]
[Authorize]
public sealed class BibliotecaController(
    RealizarPedido realizarPedido,
    IConsultasDeBiblioteca consultasDeBiblioteca) : ControllerBase
{
    /// <summary>Lista os jogos que o usuário autenticado já adquiriu.</summary>
    /// <remarks>
    /// O preço mostrado é o que foi pago na compra, congelado. Ele não muda quando o
    /// preço do catálogo muda nem quando uma promoção é encerrada.
    /// </remarks>
    /// <response code="200">Jogos da biblioteca do usuário.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemDaMinhaBiblioteca>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ItemDaMinhaBiblioteca>>> ListarMinhaBibliotecaAsync(
        CancellationToken cancellationToken)
    {
        var usuarioId = UsuarioAutenticado.ObterId(User);
        var itens = await consultasDeBiblioteca.ListarMinhaBibliotecaAsync(usuarioId, cancellationToken);

        return Ok(itens);
    }

    /// <summary>Solicita a compra; a biblioteca recebe o jogo após aprovação do pagamento.</summary>
    /// <remarks>
    /// O preço vigente é recalculado neste momento, e não reaproveitado da consulta ao
    /// catálogo — se a promoção mudou nesse intervalo, vale o preço de agora.
    /// <para>
    /// Adquirir duas vezes o mesmo jogo devolve 409 e não altera a biblioteca, então
    /// repetir a chamada por engano (duplo clique, retry) é seguro.
    /// </para>
    /// </remarks>
    /// <response code="401">Token ausente ou inválido.</response>
    /// <response code="202">Pedido pendente; consulte a URL do cabeçalho Location.</response>
    /// <response code="404">Jogo não encontrado.</response>
    /// <response code="409">O jogo já está na biblioteca ou possui pedido pendente.</response>
    /// <response code="422">O jogo não está publicado.</response>
    [HttpPost("aquisicoes")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PedidoResposta), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PedidoResposta>> AdquirirAsync(
        [FromBody] AdquirirJogoRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        // O dono da biblioteca vem sempre do token, nunca do corpo da requisição.
        var usuarioId = UsuarioAutenticado.ObterId(User);

        var aquisicao = await realizarPedido.ExecutarAsync(
            new RealizarPedidoComando(usuarioId, UsuarioAutenticado.ObterEmail(User), requisicao.JogoId),
            cancellationToken);

        return AcceptedAtAction(
            nameof(PedidosController.ObterAsync),
            "Pedidos",
            new { pedidoId = aquisicao.PedidoId },
            aquisicao);
    }
}
