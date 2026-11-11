using Fcg.Catalog.Application.Consultas;
using Fcg.Catalog.Domain.SeedWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Catalog.API.Controllers;

/// <summary>Permite acompanhar a compra sem expor pedidos de outros usuários.</summary>
[ApiController]
[Route("api/pedidos")]
[Authorize]
public sealed class PedidosController(IConsultasDePedidos consultasDePedidos) : ControllerBase
{
    /// <summary>Consulta o estado do pedido pertencente ao usuário autenticado.</summary>
    /// <response code="200">Pedido com preço congelado e resultado do pagamento.</response>
    /// <response code="401">Token ausente ou inválido.</response>
    /// <response code="404">Pedido inexistente ou pertencente a outra pessoa.</response>
    [HttpGet("{pedidoId:guid}")]
    [ProducesResponseType(typeof(PedidoDoUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDoUsuario>> ObterAsync(Guid pedidoId, CancellationToken cancellationToken)
    {
        var pedido = await consultasDePedidos.ObterPedidoDoUsuarioAsync(
            UsuarioAutenticado.ObterId(User), pedidoId, cancellationToken);

        if (pedido is null)
        {
            throw new ExcecaoDeNaoEncontrado("Pedido não encontrado.");
        }

        return Ok(pedido);
    }
}
