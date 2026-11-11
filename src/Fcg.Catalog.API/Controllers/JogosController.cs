using Fcg.Catalog.Application.Catalogo;
using Fcg.Catalog.Application.Consultas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Catalog.API.Controllers;

/// <summary>Catálogo de jogos (RF05, RF08).</summary>
[ApiController]
[Route("api/jogos")]
[Produces("application/json")]
[Authorize]
public sealed class JogosController(
    CadastrarJogo cadastrarJogo,
    AtualizarJogo atualizarJogo,
    AlterarPublicacaoDoJogo alterarPublicacao,
    IConsultasDeCatalogo consultasDeCatalogo,
    TimeProvider relogio) : ControllerBase
{
    /// <summary>Lista o catálogo público com o preço já com a promoção aplicada.</summary>
    /// <remarks>
    /// Só jogos publicados aparecem aqui: rascunho e despublicado ficam de fora.
    /// O preço vigente é calculado no instante da consulta — se a promoção acabou,
    /// o preço volta a ser o preço base automaticamente.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <param name="pagina">Página desejada, começando em 1.</param>
    /// <param name="tamanhoDaPagina">Itens por página, no máximo 100.</param>
    /// <param name="genero">Filtra por gênero, quando informado.</param>
    /// <response code="200">Página do catálogo.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<ItemDoCatalogo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ItemDoCatalogo>>> ListarCatalogoAsync(
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoDaPagina = 20,
        [FromQuery] string? genero = null)
    {
        var catalogo = await consultasDeCatalogo.ListarCatalogoPublicoAsync(
            relogio.GetUtcNow(),
            pagina,
            tamanhoDaPagina,
            genero,
            cancellationToken);

        return Ok(catalogo);
    }

    /// <summary>Detalha um jogo publicado, com o preço vigente.</summary>
    /// <response code="200">Dados do jogo.</response>
    /// <response code="404">Jogo inexistente ou fora do catálogo público.</response>
    [HttpGet("{jogoId:guid}")]
    [ProducesResponseType(typeof(ItemDoCatalogo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDoCatalogo>> ObterDetalheAsync(
        Guid jogoId,
        CancellationToken cancellationToken)
    {
        var jogo = await consultasDeCatalogo.ObterDetalheDoJogoAsync(jogoId, relogio.GetUtcNow(), cancellationToken);

        return jogo is null ? NotFound() : Ok(jogo);
    }

    /// <summary>Cadastra um jogo. Exclusivo do Administrador.</summary>
    /// <remarks>O jogo nasce em Rascunho e só aparece no catálogo depois de publicado.</remarks>
    /// <response code="201">Jogo cadastrado, em Rascunho.</response>
    /// <response code="403">O token é de um perfil Usuário.</response>
    /// <response code="409">Já existe um jogo com este título.</response>
    [HttpPost]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(typeof(JogoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JogoResposta>> CadastrarAsync(
        [FromBody] DadosDoJogoComando comando,
        CancellationToken cancellationToken)
    {
        var jogo = await cadastrarJogo.ExecutarAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObterDetalheAsync), new { jogoId = jogo.JogoId }, jogo);
    }

    /// <summary>Atualiza os dados comerciais de um jogo. Exclusivo do Administrador.</summary>
    /// <response code="200">Jogo atualizado.</response>
    /// <response code="404">Jogo não encontrado.</response>
    /// <response code="409">Outro jogo já usa este título.</response>
    [HttpPut("{jogoId:guid}")]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(typeof(JogoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JogoResposta>> AtualizarAsync(
        Guid jogoId,
        [FromBody] DadosDoJogoComando comando,
        CancellationToken cancellationToken)
    {
        var jogo = await atualizarJogo.ExecutarAsync(jogoId, comando, cancellationToken);

        return Ok(jogo);
    }

    /// <summary>Publica o jogo, colocando-o no catálogo. Exclusivo do Administrador.</summary>
    /// <response code="200">Jogo publicado.</response>
    /// <response code="422">O jogo já estava publicado.</response>
    [HttpPost("{jogoId:guid}/publicacao")]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(typeof(JogoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<JogoResposta>> PublicarAsync(Guid jogoId, CancellationToken cancellationToken)
    {
        var jogo = await alterarPublicacao.PublicarAsync(jogoId, cancellationToken);

        return Ok(jogo);
    }

    /// <summary>
    /// Retira o jogo do catálogo. Exclusivo do Administrador.
    /// Quem já o adquiriu continua com ele na biblioteca.
    /// </summary>
    /// <response code="200">Jogo despublicado.</response>
    /// <response code="422">O jogo não estava publicado.</response>
    [HttpDelete("{jogoId:guid}/publicacao")]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(typeof(JogoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<JogoResposta>> DespublicarAsync(Guid jogoId, CancellationToken cancellationToken)
    {
        var jogo = await alterarPublicacao.DespublicarAsync(jogoId, cancellationToken);

        return Ok(jogo);
    }
}
