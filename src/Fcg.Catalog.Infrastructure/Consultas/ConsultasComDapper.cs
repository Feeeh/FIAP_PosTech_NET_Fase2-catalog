using System.Data;
using Fcg.Catalog.Application.Consultas;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Fcg.Catalog.Infrastructure.Consultas;

/// <summary>
/// Abre conexões para as consultas de leitura. Uma conexão por consulta, sempre
/// descartada com <c>using</c>.
/// </summary>
public sealed class FabricaDeConexoes(string connectionString)
{
    public IDbConnection Abrir() => new SqlConnection(connectionString);
}

/// <summary>
/// Read Models do Catálogo com Dapper.
/// <para>
/// O preço vigente sai pronto do banco: um LEFT JOIN com a promoção válida no
/// instante da consulta. Fazer isso pelo ORM exigiria carregar as promoções de cada
/// jogo e calcular em memória — o N+1 que este projeto evita de propósito.
/// </para>
/// </summary>
public sealed class ConsultasDeCatalogoDapper(FabricaDeConexoes fabricaDeConexoes) : IConsultasDeCatalogo
{
    /// <summary>
    /// Junta jogo e promoção vigente. O preço vigente é calculado no próprio SQL, e
    /// arredondado com a mesma escala usada pelo Objeto de Valor Dinheiro.
    /// </summary>
    private const string SelecaoDoCatalogo = """
        SELECT
            jogo.Id                AS JogoId,
            jogo.Titulo            AS Titulo,
            jogo.Descricao         AS Descricao,
            jogo.Genero            AS Genero,
            jogo.Preco             AS PrecoBase,
            ROUND(jogo.Preco * (1 - ISNULL(promocao.PercentualDesconto, 0) / 100.0), 2) AS PrecoVigente,
            promocao.PercentualDesconto AS PercentualDesconto
        FROM Jogos AS jogo
        LEFT JOIN (
            SELECT jogoEmPromocao.JogoId, promocao.PercentualDesconto
            FROM JogosEmPromocao AS jogoEmPromocao
            INNER JOIN Promocoes AS promocao
                ON promocao.Id = jogoEmPromocao.PromocaoId
               AND promocao.Encerrada = 0
               AND @instante BETWEEN promocao.Inicio AND promocao.Fim
        ) AS promocao ON promocao.JogoId = jogo.Id
        """;

    private const int PrimeiraPagina = 1;
    private const int TamanhoMinimoDaPagina = 1;
    private const int TamanhoMaximoDaPagina = 100;

    public async Task<ResultadoPaginado<ItemDoCatalogo>> ListarCatalogoPublicoAsync(
        DateTimeOffset instante,
        int pagina,
        int tamanhoDaPagina,
        string? filtroDeGenero = null,
        CancellationToken cancellationToken = default)
    {
        var paginaSegura = Math.Max(pagina, PrimeiraPagina);
        var tamanhoSeguro = Math.Clamp(tamanhoDaPagina, TamanhoMinimoDaPagina, TamanhoMaximoDaPagina);

        // Só jogos publicados aparecem no catálogo público.
        const string filtroBase = "WHERE jogo.Status = 'Publicado'";
        var filtroDeGeneroSql = string.IsNullOrWhiteSpace(filtroDeGenero) ? string.Empty : " AND jogo.Genero = @genero";

        var sql = $"""
            {SelecaoDoCatalogo}
            {filtroBase}{filtroDeGeneroSql}
            ORDER BY jogo.Titulo
            OFFSET @deslocamento ROWS FETCH NEXT @tamanho ROWS ONLY;

            SELECT COUNT(1)
            FROM Jogos AS jogo
            {filtroBase}{filtroDeGeneroSql};
            """;

        var parametros = new
        {
            instante,
            genero = filtroDeGenero,
            deslocamento = (paginaSegura - PrimeiraPagina) * tamanhoSeguro,
            tamanho = tamanhoSeguro
        };

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, parametros, cancellationToken: cancellationToken);

        // QueryMultiple: página e total em uma única ida ao banco.
        using var resultado = await conexao.QueryMultipleAsync(comando);

        var itens = (await resultado.ReadAsync<ItemDoCatalogo>()).ToList();
        var total = await resultado.ReadFirstAsync<int>();

        return new ResultadoPaginado<ItemDoCatalogo>(itens, paginaSegura, tamanhoSeguro, total);
    }

    public async Task<ItemDoCatalogo?> ObterDetalheDoJogoAsync(
        Guid jogoId,
        DateTimeOffset instante,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {SelecaoDoCatalogo}
            WHERE jogo.Id = @jogoId AND jogo.Status = 'Publicado';
            """;

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, new { jogoId, instante }, cancellationToken: cancellationToken);

        return await conexao.QueryFirstOrDefaultAsync<ItemDoCatalogo>(comando);
    }
}

/// <summary>
/// Read Model &lt;Minha Biblioteca&gt;. O preço vem congelado do item, não do catálogo:
/// encerrar uma promoção não muda o que o usuário pagou.
/// </summary>
public sealed class ConsultasDeBibliotecaDapper(FabricaDeConexoes fabricaDeConexoes) : IConsultasDeBiblioteca
{
    public async Task<IReadOnlyCollection<ItemDaMinhaBiblioteca>> ListarMinhaBibliotecaAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                item.JogoId                   AS JogoId,
                item.TituloNoMomentoDaCompra  AS Titulo,
                item.PrecoPago                AS PrecoPago,
                item.DataAquisicao            AS DataAquisicao
            FROM ItensDeBiblioteca AS item
            WHERE item.BibliotecaId = @usuarioId
            ORDER BY item.DataAquisicao DESC;
            """;

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, new { usuarioId }, cancellationToken: cancellationToken);

        return (await conexao.QueryAsync<ItemDaMinhaBiblioteca>(comando)).ToList();
    }
}

/// <summary>Read Model &lt;Promoções Ativas&gt;.</summary>
public sealed class ConsultasDePromocoesDapper(FabricaDeConexoes fabricaDeConexoes) : IConsultasDePromocoes
{
    public async Task<IReadOnlyCollection<PromocaoAtiva>> ListarPromocoesAtivasAsync(
        DateTimeOffset instante,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                promocao.Id                 AS PromocaoId,
                promocao.PercentualDesconto AS PercentualDesconto,
                promocao.Inicio             AS Inicio,
                promocao.Fim                AS Fim,
                jogoEmPromocao.JogoId       AS JogoId
            FROM Promocoes AS promocao
            LEFT JOIN JogosEmPromocao AS jogoEmPromocao
                ON jogoEmPromocao.PromocaoId = promocao.Id
            WHERE promocao.Encerrada = 0
              AND @instante BETWEEN promocao.Inicio AND promocao.Fim
            ORDER BY promocao.Inicio;
            """;

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, new { instante }, cancellationToken: cancellationToken);

        // Uma linha por jogo da promoção: agrupa de volta em um item por promoção.
        var linhas = await conexao.QueryAsync<LinhaDePromocao>(comando);

        return linhas
            .GroupBy(linha => new { linha.PromocaoId, linha.PercentualDesconto, linha.Inicio, linha.Fim })
            .Select(grupo => new PromocaoAtiva(
                grupo.Key.PromocaoId,
                grupo.Key.PercentualDesconto,
                grupo.Key.Inicio,
                grupo.Key.Fim,
                grupo.Where(linha => linha.JogoId.HasValue).Select(linha => linha.JogoId.GetValueOrDefault()).ToList()))
            .ToList();
    }

    /// <summary>Formato cru devolvido pelo JOIN, antes do agrupamento.</summary>
    private sealed record LinhaDePromocao(
        Guid PromocaoId,
        decimal PercentualDesconto,
        DateTimeOffset Inicio,
        DateTimeOffset Fim,
        Guid? JogoId);
}

/// <summary>Consulta pedidos com Dapper, restringindo a leitura ao dono já no banco.</summary>
public sealed class ConsultasDePedidosDapper(FabricaDeConexoes fabricaDeConexoes) : IConsultasDePedidos
{
    public async Task<PedidoDoUsuario?> ObterPedidoDoUsuarioAsync(
        Guid usuarioId,
        Guid pedidoId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id AS PedidoId, JogoId, TituloDoJogo AS Titulo, Valor,
                   Status, Motivo, CriadoEm, ConcluidoEm
            FROM Pedidos
            WHERE UsuarioId = @usuarioId
              AND Id = @pedidoId;
            """;

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, new { usuarioId, pedidoId }, cancellationToken: cancellationToken);

        return await conexao.QueryFirstOrDefaultAsync<PedidoDoUsuario>(comando);
    }
}
