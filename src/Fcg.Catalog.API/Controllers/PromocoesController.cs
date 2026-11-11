using Fcg.Catalog.Application.Catalogo;
using Fcg.Catalog.Application.Consultas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Catalog.API.Controllers;

/// <summary>Promoções do catálogo (RF07). Tudo aqui é exclusivo do Administrador.</summary>
[ApiController]
[Route("api/promocoes")]
[Produces("application/json")]
[Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
public sealed class PromocoesController(
    CriarPromocao criarPromocao,
    EncerrarPromocao encerrarPromocao,
    IConsultasDePromocoes consultasDePromocoes,
    TimeProvider relogio) : ControllerBase
{
    /// <summary>Cria uma promoção sobre um ou mais jogos.</summary>
    /// <remarks>
    /// O desconto vai de 1% a 90% e o fim do período precisa ser posterior ao início.
    /// <para>
    /// Um jogo pode ter no máximo uma promoção vigente por instante: se o período
    /// informado cruzar com o de outra promoção que abrange o mesmo jogo, a criação é
    /// recusada. Isso evita a ambiguidade de "qual desconto vale".
    /// </para>
    /// </remarks>
    /// <response code="201">Promoção criada.</response>
    /// <response code="400">Período inválido ou desconto fora da faixa.</response>
    /// <response code="404">Algum dos jogos informados não existe.</response>
    /// <response code="409">Já existe promoção vigente para um dos jogos no período.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PromocaoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PromocaoResposta>> CriarAsync(
        [FromBody] CriarPromocaoComando comando,
        CancellationToken cancellationToken)
    {
        var promocao = await criarPromocao.ExecutarAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ListarAtivasAsync), null, promocao);
    }

    /// <summary>Lista as promoções que valem neste momento.</summary>
    /// <response code="200">Promoções ativas.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PromocaoAtiva>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PromocaoAtiva>>> ListarAtivasAsync(
        CancellationToken cancellationToken)
    {
        var promocoes = await consultasDePromocoes.ListarPromocoesAtivasAsync(relogio.GetUtcNow(), cancellationToken);

        return Ok(promocoes);
    }

    /// <summary>Encerra uma promoção antes do fim do período, restaurando o preço base.</summary>
    /// <remarks>
    /// Encerrar não muda nada nas bibliotecas: quem comprou com desconto continua com
    /// o preço pago que foi congelado na aquisição.
    /// </remarks>
    /// <response code="200">Promoção encerrada.</response>
    /// <response code="404">Promoção não encontrada.</response>
    /// <response code="422">A promoção já estava encerrada.</response>
    [HttpPost("{promocaoId:guid}/encerramento")]
    [ProducesResponseType(typeof(PromocaoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PromocaoResposta>> EncerrarAsync(
        Guid promocaoId,
        CancellationToken cancellationToken)
    {
        var promocao = await encerrarPromocao.ExecutarAsync(promocaoId, cancellationToken);

        return Ok(promocao);
    }
}
