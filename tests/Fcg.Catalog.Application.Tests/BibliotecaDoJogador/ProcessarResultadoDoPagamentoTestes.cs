using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Application.BibliotecaDoJogador;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Compartilhado;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fcg.Catalog.Application.Tests.BibliotecaDoJogador;

/// <summary>Verifica acesso, rejeição e reentrega sem depender do contrato do broker.</summary>
public sealed class ProcessarResultadoDoPagamentoTestes
{
    private readonly IRepositorioDePedidos _pedidos = Substitute.For<IRepositorioDePedidos>();
    private readonly IRepositorioDeBibliotecas _bibliotecas = Substitute.For<IRepositorioDeBibliotecas>();
    private readonly IUnidadeDeTrabalho _unidade = Substitute.For<IUnidadeDeTrabalho>();

    private ProcessarResultadoDoPagamento Criar() => new(
        _pedidos,
        _bibliotecas,
        _unidade,
        RelogioFalso.Padrao(),
        NullLogger<ProcessarResultadoDoPagamento>.Instance);

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Resultado_conclui_pedido_sem_duplicar_biblioteca(bool aprovado, bool jaPossui)
    {
        // Arrange
        var pedido = Pedido.Realizar(
            Guid.NewGuid(),
            "ana@fiap.com.br",
            Guid.NewGuid(),
            "Jogo",
            Dinheiro.Criar(49.90m),
            RelogioFalso.InstanteDeReferencia);
        var biblioteca = Biblioteca.Criar(pedido.UsuarioId);

        if (jaPossui)
        {
            biblioteca.Adquirir(pedido.JogoId, pedido.TituloDoJogo, pedido.Valor, pedido.CriadoEm);
        }

        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);
        _bibliotecas.ObterPorUsuarioAsync(pedido.UsuarioId, Arg.Any<CancellationToken>()).Returns(biblioteca);
        var comando = new ProcessarResultadoDoPagamentoComando(pedido.Id, aprovado, "Recusado");

        // Act
        await Criar().ExecutarAsync(comando);
        await Criar().ExecutarAsync(comando);

        // Assert
        Assert.Equal(aprovado ? StatusPedido.Aprovado : StatusPedido.Rejeitado, pedido.Status);
        Assert.Equal(aprovado ? null : "Recusado", pedido.Motivo);
        Assert.Equal(aprovado ? 1 : 0, biblioteca.Itens.Count);
        await _unidade.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Primeira_aprovacao_cria_biblioteca()
    {
        // Arrange
        var pedido = Pedido.Realizar(
            Guid.NewGuid(),
            "ana@fiap.com.br",
            Guid.NewGuid(),
            "Jogo",
            Dinheiro.Criar(49.90m),
            RelogioFalso.InstanteDeReferencia);
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        // Act
        await Criar().ExecutarAsync(new ProcessarResultadoDoPagamentoComando(pedido.Id, Aprovado: true, Motivo: null));

        // Assert
        await _bibliotecas.Received(1).AdicionarAsync(
            Arg.Is<Biblioteca>(biblioteca => biblioteca.Possui(pedido.JogoId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pedido_desconhecido_nao_altera_dados()
    {
        // Act
        await Criar().ExecutarAsync(new ProcessarResultadoDoPagamentoComando(
            Guid.NewGuid(), Aprovado: true, Motivo: null));

        // Assert
        await _unidade.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }
}
