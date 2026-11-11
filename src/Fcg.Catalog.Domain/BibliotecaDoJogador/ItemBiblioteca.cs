using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>
/// Registro de um jogo adquirido. É Entidade interna do agregado Biblioteca:
/// tem identidade para auditoria, mas só existe dentro de uma Biblioteca.
/// <para>
/// O construtor é privado de propósito — nada fora do agregado instancia um item.
/// A única porta de entrada é <see cref="Biblioteca.Adquirir"/>.
/// </para>
/// </summary>
public sealed class ItemBiblioteca : Entidade<Guid>
{
    private ItemBiblioteca()
    {
    }

    private ItemBiblioteca(Guid jogoId,
        string tituloNoMomentoDaCompra,
        Dinheiro precoPago,
        DateTimeOffset dataAquisicao)
        : base(Guid.NewGuid())
    {
        JogoId = jogoId;
        TituloNoMomentoDaCompra = tituloNoMomentoDaCompra;
        // O preço chega do Pedido; o item guarda a própria cópia (veja Dinheiro.Copiar).
        PrecoPago = precoPago.Copiar();
        DataAquisicao = dataAquisicao;
    }

    /// <summary>Referência ao agregado Jogo por identidade — nunca por navegação.</summary>
    public Guid JogoId { get; private set; }

    /// <summary>
    /// Cópia local do título. O jogo pode ser renomeado ou despublicado no Catálogo;
    /// a biblioteca continua mostrando o que o usuário comprou.
    /// </summary>
    public string TituloNoMomentoDaCompra { get; private set; } = string.Empty;

    /// <summary>Preço congelado no ato da compra. É registro histórico — nunca muda.</summary>
    public Dinheiro PrecoPago { get; private set; } = null!;

    public DateTimeOffset DataAquisicao { get; private set; }

    /// <summary>Fábrica visível apenas para o agregado, que é quem garante a invariante de duplicidade.</summary>
    internal static ItemBiblioteca Incluir(Guid jogoId,
        string tituloNoMomentoDaCompra,
        Dinheiro precoPago,
        DateTimeOffset dataAquisicao) =>
        new(jogoId, tituloNoMomentoDaCompra, precoPago, dataAquisicao);
}
