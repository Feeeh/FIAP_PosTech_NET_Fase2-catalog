using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Compartilhado;

namespace Fcg.Catalog.Domain.Tests.BibliotecaDoJogador;

/// <summary>
/// O preço congelado é guardado como cópia própria em cada agregado. Compartilhar a mesma
/// instância de Dinheiro entre Jogo, Pedido e Item faz o EF Core confundir os registros ao salvar.
/// </summary>
public sealed class PrecoCongeladoTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pedido_guarda_uma_copia_do_preco_recebido()
    {
        // Arrange
        var precoDoJogo = Dinheiro.Criar(49.90m);

        // Act
        var pedido = Pedido.Realizar(
            Guid.NewGuid(),
            "ana@fiap.com.br",
            Guid.NewGuid(),
            "Algoritmos",
            precoDoJogo,
            Agora);

        // Assert
        Assert.Equal(precoDoJogo, pedido.Valor);
        Assert.NotSame(precoDoJogo, pedido.Valor);
    }

    [Fact]
    public void Item_da_biblioteca_guarda_uma_copia_do_preco_do_pedido()
    {
        // Arrange
        var precoDoPedido = Dinheiro.Criar(49.90m);
        var biblioteca = Biblioteca.Criar(Guid.NewGuid());

        // Act
        var item = biblioteca.Adquirir(Guid.NewGuid(), "Algoritmos", precoDoPedido, Agora);

        // Assert
        Assert.Equal(precoDoPedido, item.PrecoPago);
        Assert.NotSame(precoDoPedido, item.PrecoPago);
    }
}
