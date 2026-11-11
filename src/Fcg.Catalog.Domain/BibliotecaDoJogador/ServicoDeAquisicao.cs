using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>
/// Serviço de Domínio que orquestra a aquisição (DDD.md 12.3). Atravessa Catálogo e
/// Biblioteca, por isso não é método de nenhuma das duas entidades.
/// <para>
/// Não faz I/O: recebe os agregados já carregados e devolve o pedido com preço congelado. Quem
/// busca e salva é o caso de uso — é o que mantém esta regra testável sem banco.
/// </para>
/// </summary>
public static class ServicoDeAquisicao
{
    /// <summary>
    /// Valida a aquisição entre Catálogo e Biblioteca e congela o preço no pedido,
    /// para que o pagamento não dependa de mudanças posteriores no catálogo.
    /// </summary>
    public static Pedido RealizarPedido(
        Biblioteca? biblioteca,
        Jogo jogo,
        Promocao? promocaoVigente,
        Guid usuarioId,
        string usuarioEmail,
        DateTimeOffset instante)
    {
        ArgumentNullException.ThrowIfNull(jogo);

        // Caminho de exceção "Jogo despublicado" do Event Storming da aquisição.
        jogo.GarantirQuePodeSerAdquirido();

        if (biblioteca?.Possui(jogo.Id) == true)
        {
            throw new ExcecaoDeConflito("Este jogo já está na sua biblioteca.");
        }

        // O preço é recalculado aqui, e não reaproveitado da consulta ao catálogo:
        // mitigação do risco "preço muda entre a consulta e a compra" (DDD.md seção 15).
        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, promocaoVigente, instante);

        return Pedido.Realizar(
            usuarioId,
            usuarioEmail,
            jogo.Id,
            jogo.Titulo.Valor,
            precoVigente,
            instante);
    }
}
