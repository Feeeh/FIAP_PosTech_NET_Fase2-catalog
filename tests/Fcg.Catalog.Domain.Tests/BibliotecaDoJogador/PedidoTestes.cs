using Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Tests.BibliotecaDoJogador;

/// <summary>Protege a máquina de estados contra reentrega e entradas inválidas.</summary>
public sealed class PedidoTestes
{
    private static Pedido Criar() => Pedido.Realizar(
        Guid.NewGuid(),
        "ana@fiap.com.br",
        Guid.NewGuid(),
        "Jogo",
        Dinheiro.Criar(49.90m),
        DateTimeOffset.UtcNow);

    [Fact]
    public void Pedido_nasce_pendente_com_evento()
    {
        // Arrange
        var pedido = Criar();

        // Act / Assert
        Assert.Equal(StatusPedido.Pendente, pedido.Status);
        Assert.IsType<PedidoRealizado>(Assert.Single(pedido.EventosDeDominio));
        Assert.Null(pedido.ConcluidoEm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Conclusao_e_idempotente(bool aprovado)
    {
        // Arrange
        var pedido = Criar();
        var instante = pedido.CriadoEm.AddMinutes(1);

        // Act / Assert
        Assert.True(aprovado ? pedido.Aprovar(instante) : pedido.Rejeitar("Recusado", instante));
        Assert.False(pedido.Aprovar(instante.AddMinutes(1)));
        Assert.False(pedido.Rejeitar("Outro motivo", instante.AddMinutes(1)));
        Assert.Equal(instante, pedido.ConcluidoEm);
        Assert.Equal(aprovado ? null : "Recusado", pedido.Motivo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalido")]
    public void Email_invalido_nao_cria_pedido(string email)
    {
        // Act / Assert
        Assert.Throws<ExcecaoDeValidacao>(() => Pedido.Realizar(
            Guid.NewGuid(),
            email,
            Guid.NewGuid(),
            "Jogo",
            Dinheiro.Criar(1m),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Identificador_vazio_nao_cria_pedido()
    {
        // Act / Assert
        Assert.Throws<ExcecaoDeValidacao>(() => Pedido.Realizar(
            Guid.Empty,
            "ana@fiap.com.br",
            Guid.NewGuid(),
            "Jogo",
            Dinheiro.Criar(1m),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Rejeicao_exige_motivo_e_instante_valido()
    {
        // Arrange
        var pedido = Criar();

        // Act / Assert
        Assert.Throws<ExcecaoDeValidacao>(() => pedido.Rejeitar(" ", pedido.CriadoEm));
        Assert.Throws<ExcecaoDeValidacao>(() => pedido.Aprovar(pedido.CriadoEm.AddSeconds(-1)));
        Assert.Equal(StatusPedido.Pendente, pedido.Status);
    }
}
