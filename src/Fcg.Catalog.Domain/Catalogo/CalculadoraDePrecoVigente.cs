using Fcg.Catalog.Domain.Compartilhado;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Serviço de Domínio: aplica a promoção vigente sobre o preço base de um jogo.
/// Não é método de entidade porque combina dois agregados — Jogo e Promocao.
/// </summary>
public static class CalculadoraDePrecoVigente
{
    /// <summary>
    /// Devolve o preço que o usuário paga neste instante. Sem promoção vigente que
    /// abranja o jogo, o preço vigente é o próprio preço base.
    /// </summary>
    public static Dinheiro Calcular(Jogo jogo, Promocao? promocao, DateTimeOffset instante)
    {
        ArgumentNullException.ThrowIfNull(jogo);

        var promocaoSeAplica = promocao is not null
            && promocao.EstaVigenteEm(instante)
            && promocao.AbrangeJogo(jogo.Id);

        return promocaoSeAplica
            ? jogo.Preco.AplicarDescontoPercentual(promocao!.PercentualDesconto.Valor)
            : jogo.Preco;
    }
}
