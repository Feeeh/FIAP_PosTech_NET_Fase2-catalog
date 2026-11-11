using Fcg.Catalog.Domain.BibliotecaDoJogador;

namespace Fcg.Catalog.Application.Consultas;

/// <summary>Página de resultados, para que listas grandes não voltem inteiras.</summary>
public sealed record ResultadoPaginado<T>(
    IReadOnlyCollection<T> Itens,
    int Pagina,
    int TamanhoDaPagina,
    int TotalDeItens)
{
    public int TotalDePaginas => TamanhoDaPagina <= 0 ? 0 : (int)Math.Ceiling(TotalDeItens / (double)TamanhoDaPagina);
}

/// <summary>Read Model &lt;Catálogo Público&gt;: um jogo publicado com o preço já com desconto.</summary>
public sealed record ItemDoCatalogo(
    Guid JogoId,
    string Titulo,
    string Descricao,
    string Genero,
    decimal PrecoBase,
    decimal PrecoVigente,
    decimal? PercentualDesconto);

/// <summary>Read Model &lt;Minha Biblioteca&gt;: o preço aqui é o congelado na compra.</summary>
public sealed record ItemDaMinhaBiblioteca(
    Guid JogoId,
    string Titulo,
    decimal PrecoPago,
    DateTimeOffset DataAquisicao);

/// <summary>Read Model &lt;Promoções Ativas&gt;.</summary>
public sealed record PromocaoAtiva(
    Guid PromocaoId,
    decimal PercentualDesconto,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    IReadOnlyCollection<Guid> JogosIds);

/// <summary>Leitura do pedido com preço congelado para acompanhar a decisão do pagamento.</summary>
public sealed record PedidoDoUsuario(
    Guid PedidoId,
    Guid JogoId,
    string Titulo,
    decimal Valor,
    StatusPedido Status,
    string? Motivo,
    DateTimeOffset CriadoEm,
    DateTimeOffset? ConcluidoEm);
