using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Intervalo de tempo fechado no início e no fim. Objeto de Valor que valida a
/// própria consistência: não existe período com fim antes do início.
/// </summary>
public sealed class Periodo : ObjetoDeValor
{
    private Periodo()
    {
    }

    private Periodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    public DateTimeOffset Inicio { get; private set; }

    public DateTimeOffset Fim { get; private set; }

    public static Periodo Criar(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio)
        {
            throw new ExcecaoDeValidacao("O fim do período precisa ser posterior ao início.");
        }

        return new Periodo(inicio, fim);
    }

    /// <summary>Diz se um instante cai dentro deste período.</summary>
    public bool Contem(DateTimeOffset instante) => instante >= Inicio && instante <= Fim;

    /// <summary>
    /// Diz se dois períodos se cruzam em algum instante. É o que sustenta a regra
    /// "um jogo tem no máximo uma promoção vigente por instante".
    /// </summary>
    public bool SobrepoeCom(Periodo outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Inicio <= outro.Fim && outro.Inicio <= Fim;
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Inicio;
        yield return Fim;
    }

    public override string ToString() => $"{Inicio:yyyy-MM-dd HH:mm} a {Fim:yyyy-MM-dd HH:mm}";
}
