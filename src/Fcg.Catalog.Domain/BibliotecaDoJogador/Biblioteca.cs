using Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>
/// Core Domain. Conjunto de jogos que um usuário já adquiriu — pessoal e permanente.
/// A identidade da biblioteca é a do dono, então existe no máximo uma por usuário.
/// <para>
/// Invariantes (DDD.md 12.1):
/// o mesmo JogoId nunca aparece duas vezes; um item incluído não é removido nem
/// alterado; o preço pago é imutável.
/// </para>
/// </summary>
public sealed class Biblioteca : RaizDeAgregado<Guid>
{
    private readonly List<ItemBiblioteca> _itens = [];

    private Biblioteca()
    {
    }

    private Biblioteca(Guid usuarioId) : base(usuarioId)
    {
    }

    /// <summary>A identidade da biblioteca é a do dono (DDD.md 12.2).</summary>
    public Guid UsuarioId => Id;

    /// <summary>
    /// Somente leitura de propósito: incluir item só acontece por <see cref="Adquirir"/>,
    /// que é onde a invariante de duplicidade é verificada.
    /// </summary>
    public IReadOnlyCollection<ItemBiblioteca> Itens => _itens.AsReadOnly();

    public static Biblioteca Criar(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeValidacao("A biblioteca precisa de um usuário dono.");
        }

        return new Biblioteca(usuarioId);
    }

    /// <summary>Responde se o jogo já está na biblioteca, sem expor a coleção interna.</summary>
    public bool Possui(Guid jogoId) => _itens.Exists(item => item.JogoId == jogoId);

    /// <summary>
    /// Única forma de incluir um jogo. Recebe o preço já calculado pelo Catálogo
    /// (preço vigente no instante da compra) e o congela no item.
    /// </summary>
    /// <exception cref="ExcecaoDeConflito">Quando o jogo já está na biblioteca.</exception>
    public ItemBiblioteca Adquirir(Guid jogoId,
        string tituloDoJogo,
        Dinheiro precoVigente,
        DateTimeOffset dataAquisicao)
    {
        if (jogoId == Guid.Empty)
        {
            throw new ExcecaoDeValidacao("O jogo a adquirir é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(tituloDoJogo))
        {
            throw new ExcecaoDeValidacao("O título do jogo é obrigatório.");
        }

        ArgumentNullException.ThrowIfNull(precoVigente);

        // Invariante que é a razão de ser deste agregado. Verificada antes de qualquer
        // alteração, para que uma aquisição rejeitada deixe a biblioteca intacta.
        if (Possui(jogoId))
        {
            throw new ExcecaoDeConflito("Este jogo já está na sua biblioteca.");
        }

        var item = ItemBiblioteca.Incluir(jogoId, tituloDoJogo.Trim(), precoVigente, dataAquisicao);
        _itens.Add(item);

        RegistrarEvento(new JogoAdquirido(UsuarioId, jogoId, precoVigente.Valor) { OcorridoEm = dataAquisicao });
        RegistrarEvento(new ItemDeBibliotecaIncluido(UsuarioId,
            jogoId,
            item.TituloNoMomentoDaCompra)
        { OcorridoEm = dataAquisicao });

        return item;
    }
}
