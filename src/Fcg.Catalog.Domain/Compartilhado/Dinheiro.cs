using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Compartilhado;

/// <summary>
/// Objeto de Valor que representa uma quantia. Faz parte do Shared Kernel: é usado
/// pelo Catálogo (preço do jogo) e pela Biblioteca (preço pago, congelado).
/// R$ 49,90 é igual a qualquer outro R$ 49,90 — por isso é Objeto de Valor.
/// </summary>
public sealed class Dinheiro : ObjetoDeValor
{
    /// <summary>Única moeda aceita na Fase 1.</summary>
    public const string MoedaPadrao = "BRL";

    private const int CasasDecimaisDeMoeda = 2;

    private Dinheiro()
    {
    }

    private Dinheiro(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public decimal Valor { get; private set; }

    public string Moeda { get; private set; } = MoedaPadrao;

    /// <summary>
    /// Cria uma quantia. Invariante do agregado Jogo: preço não pode ser negativo —
    /// garantida aqui, então nenhum agregado precisa repetir essa checagem.
    /// </summary>
    public static Dinheiro Criar(decimal valor, string moeda = MoedaPadrao)
    {
        if (valor < 0)
        {
            throw new ExcecaoDeValidacao("O valor não pode ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(moeda))
        {
            throw new ExcecaoDeValidacao("A moeda é obrigatória.");
        }

        return new Dinheiro(Math.Round(valor,
            CasasDecimaisDeMoeda,
            MidpointRounding.ToEven),
            moeda.Trim().ToUpperInvariant());
    }

    public static Dinheiro Zero(string moeda = MoedaPadrao) => Criar(0m, moeda);

    /// <summary>
    /// Devolve uma quantia igual, mas em outra instância.
    /// <para>
    /// No banco, cada Dinheiro é gravado como parte do agregado que o contém (owned type do
    /// EF Core). Uma mesma instância não pode pertencer a dois agregados: sem o preço do jogo
    /// e o do pedido separados, o EF Core os confundiria ao salvar. Quem guarda um Dinheiro
    /// recebido de outro agregado guarda uma cópia.
    /// </para>
    /// </summary>
    public Dinheiro Copiar() => new(Valor, Moeda);

    /// <summary>
    /// Aplica um desconto percentual e devolve uma nova quantia — o original é imutável.
    /// É assim que a CalculadoraDePrecoVigente monta o preço com promoção.
    /// </summary>
    public Dinheiro AplicarDescontoPercentual(decimal percentual)
    {
        if (percentual is < 0 or > 100)
        {
            throw new ExcecaoDeValidacao("O percentual de desconto deve estar entre 0 e 100.");
        }

        var valorComDesconto = Valor * (1 - (percentual / 100m));
        return Criar(valorComDesconto, Moeda);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
        yield return Moeda;
    }

    public override string ToString() => $"{Moeda} {Valor:N2}";
}
