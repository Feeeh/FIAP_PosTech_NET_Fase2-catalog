using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Percentual de desconto de uma promoção. A faixa 1..90 vem do DDD.md 12.1:
/// desconto de 0% não é promoção, e acima de 90% seria erro de digitação.
/// </summary>
public sealed class PercentualDesconto : ObjetoDeValor
{
    public const decimal PercentualMinimo = 1m;
    public const decimal PercentualMaximo = 90m;

    private PercentualDesconto()
    {
    }

    private PercentualDesconto(decimal valor) => Valor = valor;

    public decimal Valor { get; private set; }

    public static PercentualDesconto Criar(decimal valor)
    {
        if (valor < PercentualMinimo || valor > PercentualMaximo)
        {
            throw new ExcecaoDeValidacao(
                $"O desconto precisa estar entre {PercentualMinimo:N0}% e {PercentualMaximo:N0}%.");
        }

        return new PercentualDesconto(valor);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => $"{Valor:N0}%";
}
