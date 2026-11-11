namespace Fcg.Catalog.Application.Consultas;

/// <summary>
/// Consultas do Catálogo. Separadas dos repositórios de propósito: são leitura pura,
/// implementadas com Dapper porque o preço vigente é um JOIN entre Jogo e Promoção —
/// materializar isso pelo ORM traria N+1.
/// </summary>
public interface IConsultasDeCatalogo
{
    Task<ResultadoPaginado<ItemDoCatalogo>> ListarCatalogoPublicoAsync(
        DateTimeOffset instante,
        int pagina,
        int tamanhoDaPagina,
        string? filtroDeGenero = null,
        CancellationToken cancellationToken = default);

    Task<ItemDoCatalogo?> ObterDetalheDoJogoAsync(
        Guid jogoId,
        DateTimeOffset instante,
        CancellationToken cancellationToken = default);
}

/// <summary>Consultas da Biblioteca do usuário autenticado.</summary>
public interface IConsultasDeBiblioteca
{
    Task<IReadOnlyCollection<ItemDaMinhaBiblioteca>> ListarMinhaBibliotecaAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}

/// <summary>Consultas de promoções.</summary>
public interface IConsultasDePromocoes
{
    Task<IReadOnlyCollection<PromocaoAtiva>> ListarPromocoesAtivasAsync(
        DateTimeOffset instante,
        CancellationToken cancellationToken = default);
}

/// <summary>Consulta pedidos do dono para acompanhar a compra sem expor dados de outros usuários.</summary>
public interface IConsultasDePedidos
{
    Task<PedidoDoUsuario?> ObterPedidoDoUsuarioAsync(
        Guid usuarioId,
        Guid pedidoId,
        CancellationToken cancellationToken = default);
}
