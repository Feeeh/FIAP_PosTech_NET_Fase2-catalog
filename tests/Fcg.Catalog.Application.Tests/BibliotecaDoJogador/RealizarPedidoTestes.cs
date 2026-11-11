using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Application.BibliotecaDoJogador;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fcg.Catalog.Application.Tests.BibliotecaDoJogador;

/// <summary>RF09 — a orquestração da aquisição, o caso de uso do Core Domain.</summary>
public class RealizarPedidoTestes
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IRepositorioDeBibliotecas _repositorioDeBibliotecas = Substitute.For<IRepositorioDeBibliotecas>();
    private readonly IRepositorioDeJogos _repositorioDeJogos = Substitute.For<IRepositorioDeJogos>();
    private readonly IRepositorioDePromocoes _repositorioDePromocoes = Substitute.For<IRepositorioDePromocoes>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();

    private readonly IRepositorioDePedidos _pedidos = Substitute.For<IRepositorioDePedidos>();

    private RealizarPedido CriarCasoDeUso() => new(
        _repositorioDeBibliotecas,
        _repositorioDeJogos,
        _repositorioDePromocoes,
        _pedidos,
        _unidadeDeTrabalho,
        RelogioFalso.Padrao(),
        NullLogger<RealizarPedido>.Instance);

    private static Jogo CriarJogoPublicado(decimal preco = 80m)
    {
        var jogo = Jogo.Cadastrar(
            Titulo.Criar("Hollow Knight"),
            Descricao.Criar("Metroidvania desenhado à mão."),
            Dinheiro.Criar(preco),
            Genero.Criar("Aventura"),
            RelogioFalso.InstanteDeReferencia);

        jogo.Publicar();
        return jogo;
    }

    private void DadoQueOJogoExiste(Jogo jogo) =>
        _repositorioDeJogos.ObterPorIdAsync(jogo.Id, Arg.Any<CancellationToken>()).Returns(jogo);

    [Fact]
    public async Task Primeira_aquisicao_cria_pedido_pendente()
    {
        // Arrange
        var jogo = CriarJogoPublicado();
        DadoQueOJogoExiste(jogo);
        _repositorioDeBibliotecas.ObterPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns((Biblioteca?)null);

        // Act
        var resposta = await CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId,
            "ana@fiap.com.br",
            jogo.Id));

        // Assert
        Assert.Equal(jogo.Id, resposta.JogoId);
        Assert.Equal(80m, resposta.Valor);
        Assert.Equal(StatusPedido.Pendente, resposta.Status);
        await _pedidos.Received(1).AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
        await _unidadeDeTrabalho.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Aquisicao_em_biblioteca_existente_nao_cria_outra()
    {
        // Arrange
        var jogo = CriarJogoPublicado();
        DadoQueOJogoExiste(jogo);
        _repositorioDeBibliotecas.ObterPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns(Biblioteca.Criar(UsuarioId));

        // Act
        await CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId, "ana@fiap.com.br", jogo.Id));

        // Assert
        await _repositorioDeBibliotecas.DidNotReceive()
            .AdicionarAsync(Arg.Any<Biblioteca>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Jogo_inexistente_devolve_nao_encontrado()
    {
        // Arrange
        _repositorioDeJogos.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Jogo?)null);

        // Act / Assert
        await Assert.ThrowsAsync<ExcecaoDeNaoEncontrado>(
            () => CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId,
                "ana@fiap.com.br",
                Guid.NewGuid())));

        await _unidadeDeTrabalho.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Aquisicao_repetida_e_rejeitada_e_nada_e_salvo()
    {
        // Arrange
        var jogo = CriarJogoPublicado();
        DadoQueOJogoExiste(jogo);

        var bibliotecaQueJaTemOJogo = Biblioteca.Criar(UsuarioId);
        bibliotecaQueJaTemOJogo.Adquirir(
            jogo.Id,
            jogo.Titulo.Valor,
            Dinheiro.Criar(80m),
            RelogioFalso.InstanteDeReferencia);

        _repositorioDeBibliotecas.ObterPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns(bibliotecaQueJaTemOJogo);

        // Act / Assert
        await Assert.ThrowsAsync<ExcecaoDeConflito>(
            () => CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId, "ana@fiap.com.br", jogo.Id)));

        Assert.Single(bibliotecaQueJaTemOJogo.Itens);
        await _unidadeDeTrabalho.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Jogo_despublicado_nao_pode_ser_adquirido()
    {
        // Arrange
        var jogo = CriarJogoPublicado();
        // Act
        jogo.Despublicar();
        DadoQueOJogoExiste(jogo);
        _repositorioDeBibliotecas.ObterPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns((Biblioteca?)null);

        // Assert
        await Assert.ThrowsAsync<ExcecaoDeEstadoInvalido>(
            () => CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId, "ana@fiap.com.br", jogo.Id)));

        await _unidadeDeTrabalho.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task O_preco_e_recalculado_no_instante_da_compra_com_a_promocao_vigente()
    {
        // Arrange
        var jogo = CriarJogoPublicado(preco: 80m);
        DadoQueOJogoExiste(jogo);
        _repositorioDeBibliotecas.ObterPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns((Biblioteca?)null);

        var promocao = Promocao.Criar(
            PercentualDesconto.Criar(25m),
            Periodo.Criar(
                RelogioFalso.InstanteDeReferencia.AddDays(-1),
                RelogioFalso.InstanteDeReferencia.AddDays(1)),
            [jogo.Id],
            RelogioFalso.InstanteDeReferencia.AddDays(-2));

        _repositorioDePromocoes
            .ObterVigenteParaJogoAsync(jogo.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(promocao);

        // Act
        var resposta = await CriarCasoDeUso().ExecutarAsync(new RealizarPedidoComando(UsuarioId,
            "ana@fiap.com.br",
            jogo.Id));

        promocao.Encerrar();
        jogo.Atualizar(jogo.Titulo, jogo.Descricao, Dinheiro.Criar(200m), jogo.Genero);

        // Assert
        Assert.Equal(60m, resposta.Valor);
        await _pedidos.Received(1).AdicionarAsync(
            Arg.Is<Pedido>(pedido => pedido.Valor.Valor == 60m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pedido_pendente_impede_nova_compra()
    {
        // Arrange
        var jogo = CriarJogoPublicado();
        DadoQueOJogoExiste(jogo);
        _pedidos.ExistePendenteAsync(UsuarioId, jogo.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act / Assert
        await Assert.ThrowsAsync<ExcecaoDeConflito>(() => CriarCasoDeUso().ExecutarAsync(
            new RealizarPedidoComando(UsuarioId, "ana@fiap.com.br", jogo.Id)));

        await _unidadeDeTrabalho.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }
}
