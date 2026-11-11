using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.SeedWork;
using Fcg.Catalog.Domain.Tests.Catalogo;

namespace Fcg.Catalog.Domain.Tests.BibliotecaDoJogador;

/// <summary>
/// O fluxo de aquisição ponta a ponta dentro do domínio — sem banco, sem HTTP.
/// Cobre os dois caminhos de exceção do Event Storming da seção 9 do DDD.md.
/// </summary>
public class ServicoDeAquisicaoTestes
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DentroDoPeriodo = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

    private static Promocao CriarPromocaoDe(decimal percentual, Guid jogoId) =>
        Promocao.Criar(
            PercentualDesconto.Criar(percentual),
            Periodo.Criar(Inicio, Fim),
            [jogoId],
            Inicio.AddDays(-1));

    [Fact]
    public void Realiza_pedido_de_jogo_publicado_pelo_preco_base_sem_promocao()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogo = JogoTestes.CriarJogoPublicado(preco: 80m);

        // Act
        var pedido = ServicoDeAquisicao.RealizarPedido(
            biblioteca,
            jogo,
            null,
            UsuarioId,
            "ana@fiap.com.br",
            DentroDoPeriodo);

        // Assert
        Assert.Equal(80m, pedido.Valor.Valor);
        Assert.False(biblioteca.Possui(jogo.Id));
    }

    [Fact]
    public void Realiza_pedido_com_o_desconto_da_promocao_vigente()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogo = JogoTestes.CriarJogoPublicado(preco: 80m);
        var promocao = CriarPromocaoDe(25m, jogo.Id);

        // Act
        var pedido = ServicoDeAquisicao.RealizarPedido(
            biblioteca,
            jogo,
            promocao,
            UsuarioId,
            "ana@fiap.com.br",
            DentroDoPeriodo);

        // Assert
        Assert.Equal(60m, pedido.Valor.Valor);
    }

    [Fact]
    public void Nao_realiza_pedido_para_jogo_em_rascunho()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogoEmRascunho = JogoTestes.CriarJogoEmRascunho();

        // Act / Assert
        Assert.Throws<ExcecaoDeEstadoInvalido>(
            () => ServicoDeAquisicao.RealizarPedido(
                biblioteca,
                jogoEmRascunho,
                null,
                UsuarioId,
                "ana@fiap.com.br",
                DentroDoPeriodo));

        Assert.Empty(biblioteca.Itens);
    }

    [Fact]
    public void Nao_realiza_pedido_para_jogo_despublicado()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogo = JogoTestes.CriarJogoPublicado();
        // Act
        jogo.Despublicar();

        // Assert
        Assert.Throws<ExcecaoDeEstadoInvalido>(
            () => ServicoDeAquisicao.RealizarPedido(
                biblioteca,
                jogo,
                null,
                UsuarioId,
                "ana@fiap.com.br",
                DentroDoPeriodo));

        Assert.Empty(biblioteca.Itens);
    }

    [Fact]
    public void Nao_realiza_pedido_para_duas_vezes_o_mesmo_jogo()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogo = JogoTestes.CriarJogoPublicado();
        biblioteca.Adquirir(jogo.Id, jogo.Titulo.Valor, jogo.Preco, DentroDoPeriodo);

        // Act / Assert
        Assert.Throws<ExcecaoDeConflito>(
            () => ServicoDeAquisicao.RealizarPedido(
                biblioteca,
                jogo,
                null,
                UsuarioId,
                "ana@fiap.com.br",
                DentroDoPeriodo));
    }

    [Fact]
    public void Encerrar_a_promocao_depois_da_compra_nao_muda_o_preco_pago()
    {
        // Arrange
        var biblioteca = Biblioteca.Criar(UsuarioId);
        var jogo = JogoTestes.CriarJogoPublicado(preco: 80m);
        var promocao = CriarPromocaoDe(25m, jogo.Id);

        // Act
        var pedido = ServicoDeAquisicao.RealizarPedido(
            biblioteca,
            jogo,
            promocao,
            UsuarioId,
            "ana@fiap.com.br",
            DentroDoPeriodo);
        promocao.Encerrar();

        // O pedido guarda o histórico: o preço solicitado é imutável.
        // Assert
        Assert.Equal(60m, pedido.Valor.Valor);
    }

    [Fact]
    public void Despublicar_o_jogo_depois_do_pedido_preserva_os_dados_da_compra()
    {
        // Arrange
        var jogo = JogoTestes.CriarJogoPublicado();
        // Act
        var pedido = ServicoDeAquisicao.RealizarPedido(
            null,
            jogo,
            null,
            UsuarioId,
            "ana@fiap.com.br",
            DentroDoPeriodo);

        jogo.Despublicar();

        // Assert
        Assert.Equal(jogo.Id, pedido.JogoId);
        Assert.Equal(StatusPedido.Pendente, pedido.Status);
        Assert.Equal(jogo.Titulo.Valor, pedido.TituloDoJogo);
    }
}
