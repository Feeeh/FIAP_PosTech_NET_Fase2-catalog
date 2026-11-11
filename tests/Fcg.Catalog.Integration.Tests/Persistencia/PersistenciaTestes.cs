using System.Text.Json;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Application.Consultas;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;
using Fcg.Catalog.Infrastructure.Consultas;
using Fcg.Catalog.Infrastructure.Mensageria;
using Fcg.Catalog.Infrastructure.Persistencia;
using Fcg.Catalog.Infrastructure.Repositorios;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Sdk;

namespace Fcg.Catalog.Integration.Tests.Persistencia;

/// <summary>
/// Integra EF Core, migrations, repositórios, Unit of Work e as consultas Dapper
/// contra SQL Server real. Cada teste recebe um banco isolado e descartável.
/// </summary>
public sealed class PersistenciaTestes : IAsyncLifetime
{
    private readonly string _nomeDoBanco = $"fcg_integracao_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _connectionString = CriarConnectionString();
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return;
        }

        await using var contexto = CriarContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Catalogo_nao_duplica_jogo_por_promocao_historica()
    {
        // Arrange
        var instante = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var jogo = Jogo.Cadastrar(
            Titulo.Criar("Hollow Knight"),
            Descricao.Criar("Metroidvania desenhado à mão."),
            Dinheiro.Criar(100m),
            Genero.Criar("Aventura"),
            instante.AddMonths(-1));
        jogo.Publicar();

        var promocaoHistorica = Promocao.Criar(
            PercentualDesconto.Criar(10m),
            Periodo.Criar(instante.AddMonths(-2), instante.AddMonths(-1)),
            [jogo.Id],
            instante.AddMonths(-2));
        var promocaoVigente = Promocao.Criar(
            PercentualDesconto.Criar(25m),
            Periodo.Criar(instante.AddDays(-1), instante.AddDays(1)),
            [jogo.Id],
            instante.AddDays(-2));

        await using (var contexto = CriarContexto())
        {
            contexto.AddRange(jogo, promocaoHistorica, promocaoVigente);
            await contexto.SaveChangesAsync();
        }

        IConsultasDeCatalogo consultas = new ConsultasDeCatalogoDapper(new FabricaDeConexoes(_connectionString));
        // Act
        var catalogo = await consultas.ListarCatalogoPublicoAsync(instante, 1, 20);

        // Assert
        var item = Assert.Single(catalogo.Itens);
        Assert.Equal(75m, item.PrecoVigente);
        Assert.Equal(1, catalogo.TotalDeItens);
    }

    [Fact]
    public async Task Pedido_e_outbox_sao_salvos_juntos_com_correlacao()
    {
        // Arrange
        await using var contexto = CriarContexto();
        var pedido = CriarPedido(Guid.NewGuid(), Guid.NewGuid());
        contexto.Pedidos.Add(pedido);
        var correlacao = new ContextoDeCorrelacao();
        correlacao.Definir("teste-pedido");
        var unidade = new UnidadeDeTrabalho(contexto, correlacao);

        // Act
        await unidade.SalvarAlteracoesAsync();
        contexto.ChangeTracker.Clear();

        var mensagem = await contexto.MensagensDeSaida.SingleAsync();
        using var json = JsonDocument.Parse(mensagem.Conteudo);
        // Assert
        Assert.Equal("teste-pedido", mensagem.CorrelationId);
        Assert.Equal("order.placed", mensagem.RoutingKey);
        Assert.Equal(pedido.Id, json.RootElement.GetProperty("orderId").GetGuid());
        Assert.Equal(49.90m, json.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal("ana@fiap.com.br", json.RootElement.GetProperty("userEmail").GetString());
        Assert.Equal("Jogo", json.RootElement.GetProperty("gameTitle").GetString());
        Assert.Equal(mensagem.Id, json.RootElement.GetProperty("eventId").GetGuid());
    }

    [Fact]
    public async Task Indice_impede_segundo_pendente_e_permite_novo_apos_rejeicao()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        await using var contexto = CriarContexto();
        var primeiro = CriarPedido(usuarioId, jogoId);
        contexto.Pedidos.Add(primeiro);
        await contexto.SaveChangesAsync();
        contexto.Pedidos.Add(CriarPedido(usuarioId, jogoId));

        // Act / Assert
        await Assert.ThrowsAsync<ExcecaoDeConflito>(() => new UnidadeDeTrabalho(
            contexto, new ContextoDeCorrelacao()).SalvarAlteracoesAsync());

        contexto.ChangeTracker.Clear();
        var persistido = await contexto.Pedidos.SingleAsync();
        persistido.Rejeitar("Recusado", persistido.CriadoEm.AddMinutes(1));
        await contexto.SaveChangesAsync();
        contexto.Pedidos.Add(CriarPedido(usuarioId, jogoId));
        await contexto.SaveChangesAsync();

        Assert.Equal(2, await contexto.Pedidos.CountAsync());
    }

    [Fact]
    public async Task Biblioteca_persistida_mantem_preco_congelado_na_consulta()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(Guid.NewGuid());
        biblioteca.Adquirir(Guid.NewGuid(), "Jogo", Dinheiro.Criar(49.90m), DateTimeOffset.UtcNow);
        await using var contexto = CriarContexto();
        contexto.Bibliotecas.Add(biblioteca);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        // Act
        var persistida = await new RepositorioDeBibliotecas(contexto).ObterPorUsuarioAsync(biblioteca.UsuarioId);
        // Assert
        Assert.NotNull(persistida);
        Assert.Single(persistida.Itens);
        var consulta = new ConsultasDeBibliotecaDapper(new FabricaDeConexoes(_connectionString));
        var item = Assert.Single(await consulta.ListarMinhaBibliotecaAsync(biblioteca.UsuarioId));
        Assert.Equal(49.90m, item.PrecoPago);
    }

    [Fact]
    public async Task Consulta_de_pedido_traz_todos_os_campos_apenas_para_o_dono()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var pedido = CriarPedido(usuarioId, Guid.NewGuid());
        pedido.Rejeitar("Pagamento recusado", pedido.CriadoEm.AddMinutes(1));
        await using var contexto = CriarContexto();
        contexto.Pedidos.Add(pedido);
        await contexto.SaveChangesAsync();
        IConsultasDePedidos consultas = new ConsultasDePedidosDapper(new FabricaDeConexoes(_connectionString));

        // Act
        var leitura = await consultas.ObterPedidoDoUsuarioAsync(usuarioId, pedido.Id);
        var leituraDeOutroUsuario = await consultas.ObterPedidoDoUsuarioAsync(Guid.NewGuid(), pedido.Id);
        var inexistente = await consultas.ObterPedidoDoUsuarioAsync(usuarioId, Guid.NewGuid());

        // Assert
        Assert.NotNull(leitura);
        Assert.Equal(pedido.Id, leitura.PedidoId);
        Assert.Equal(pedido.JogoId, leitura.JogoId);
        Assert.Equal(pedido.TituloDoJogo, leitura.Titulo);
        Assert.Equal(pedido.Valor.Valor, leitura.Valor);
        Assert.Equal(StatusPedido.Rejeitado, leitura.Status);
        Assert.Equal(pedido.Motivo, leitura.Motivo);
        Assert.Equal(pedido.CriadoEm, leitura.CriadoEm);
        Assert.Equal(pedido.ConcluidoEm, leitura.ConcluidoEm);
        Assert.Null(leituraDeOutroUsuario);
        Assert.Null(inexistente);
    }

    private static Pedido CriarPedido(Guid usuarioId, Guid jogoId) => Pedido.Realizar(
        usuarioId,
        "ana@fiap.com.br",
        jogoId,
        "Jogo",
        Dinheiro.Criar(49.90m),
        DateTimeOffset.UtcNow);

    private CatalogoDbContext CriarContexto() => new(
        new DbContextOptionsBuilder<CatalogoDbContext>()
            .UseSqlServer(_connectionString)
            .Options);

    private string CriarConnectionString()
    {
        var servidorConfigurado = Environment.GetEnvironmentVariable("FCG_TEST_SQLSERVER");

        if (string.IsNullOrWhiteSpace(servidorConfigurado))
        {
            if (!OperatingSystem.IsWindows())
            {
                throw SkipException.ForSkip(
                    "Defina FCG_TEST_SQLSERVER com uma conexão SQL Server de teste fora do Windows.");
            }

            servidorConfigurado = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";
        }

        var construtor = new SqlConnectionStringBuilder(servidorConfigurado)
        {
            InitialCatalog = _nomeDoBanco
        };

        return construtor.ConnectionString;
    }

}
