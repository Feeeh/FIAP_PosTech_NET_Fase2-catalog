using Fcg.Catalog.Domain.Catalogo.Eventos;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Referência a um jogo abrangido por uma promoção. Guarda apenas o Id: Jogo é
/// outro agregado, e agregados se referenciam por identidade, nunca por navegação.
/// </summary>
public sealed class JogoEmPromocao
{
    private JogoEmPromocao()
    {
    }

    private JogoEmPromocao(Guid jogoId) => JogoId = jogoId;

    public Guid JogoId { get; private set; }

    internal static JogoEmPromocao Referenciar(Guid jogoId) => new(jogoId);
}

/// <summary>
/// Desconto percentual sobre um ou mais jogos, válido em um período.
/// <para>
/// Invariantes (DDD.md 12.1): período com fim posterior ao início (garantido por
/// <see cref="Periodo"/>); desconto dentro da faixa (garantido por
/// <see cref="PercentualDesconto"/>); um jogo tem no máximo uma promoção vigente
/// por instante — esta última é verificada em <see cref="ConflitaCom"/>, porque
/// depende das outras promoções e não só desta.
/// </para>
/// </summary>
public sealed class Promocao : RaizDeAgregado<Guid>
{
    private readonly List<JogoEmPromocao> _jogos = [];

    private Promocao()
    {
    }

    private Promocao(Guid id, PercentualDesconto percentualDesconto, Periodo periodo, DateTimeOffset dataDeCriacao)
        : base(id)
    {
        PercentualDesconto = percentualDesconto;
        Periodo = periodo;
        DataDeCriacao = dataDeCriacao;
        Encerrada = false;
    }

    public PercentualDesconto PercentualDesconto { get; private set; } = null!;

    public Periodo Periodo { get; private set; } = null!;

    /// <summary>Encerrada manualmente pelo administrador, antes mesmo do fim do período.</summary>
    public bool Encerrada { get; private set; }

    public DateTimeOffset DataDeCriacao { get; private set; }

    public IReadOnlyCollection<JogoEmPromocao> Jogos => _jogos.AsReadOnly();

    /// <summary>Ids dos jogos abrangidos, no formato que os outros contextos consomem.</summary>
    public IReadOnlyCollection<Guid> JogosIds => _jogos.Select(jogo => jogo.JogoId).ToList().AsReadOnly();

    public static Promocao Criar(
        PercentualDesconto percentualDesconto,
        Periodo periodo,
        IEnumerable<Guid> jogosIds,
        DateTimeOffset dataDeCriacao)
    {
        ArgumentNullException.ThrowIfNull(percentualDesconto);
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(jogosIds);

        var idsUnicos = jogosIds.Where(id => id != Guid.Empty).Distinct().ToList();

        if (idsUnicos.Count == 0)
        {
            throw new ExcecaoDeValidacao("A promoção precisa abranger ao menos um jogo.");
        }

        var promocao = new Promocao(Guid.NewGuid(), percentualDesconto, periodo, dataDeCriacao);
        promocao._jogos.AddRange(idsUnicos.Select(JogoEmPromocao.Referenciar));
        promocao.RegistrarEvento(
            new PromocaoCriada(promocao.Id, percentualDesconto.Valor, idsUnicos) { OcorridoEm = dataDeCriacao });

        return promocao;
    }

    public bool AbrangeJogo(Guid jogoId) => _jogos.Exists(jogo => jogo.JogoId == jogoId);

    /// <summary>Diz se a promoção vale neste instante — encerrada nunca vale.</summary>
    public bool EstaVigenteEm(DateTimeOffset instante) => !Encerrada && Periodo.Contem(instante);

    /// <summary>
    /// Diz se esta promoção brigaria com outra: mesmo jogo, períodos que se cruzam.
    /// Uma promoção encerrada não conflita com nada.
    /// </summary>
    public bool ConflitaCom(Promocao outra)
    {
        ArgumentNullException.ThrowIfNull(outra);

        if (Encerrada || outra.Encerrada)
        {
            return false;
        }

        return Periodo.SobrepoeCom(outra.Periodo) && JogosIds.Intersect(outra.JogosIds).Any();
    }

    /// <summary>Encerra a promoção, restaurando o preço base dos jogos abrangidos.</summary>
    /// <exception cref="ExcecaoDeEstadoInvalido">Quando já estava encerrada.</exception>
    public void Encerrar()
    {
        if (Encerrada)
        {
            throw new ExcecaoDeEstadoInvalido("Esta promoção já está encerrada.");
        }

        Encerrada = true;
        RegistrarEvento(new PromocaoEncerrada(Id));
    }
}
