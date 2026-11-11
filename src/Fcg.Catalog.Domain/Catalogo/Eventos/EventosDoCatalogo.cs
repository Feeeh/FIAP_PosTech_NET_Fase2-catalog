using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo.Eventos;

/// <summary>Jogo criado no catálogo, ainda em rascunho.</summary>
public sealed record JogoCadastrado(Guid JogoId, string Titulo) : EventoDeDominio;

/// <summary>Dados comerciais do jogo mudaram (título, descrição, preço ou gênero).</summary>
public sealed record JogoAtualizado(Guid JogoId, string Titulo) : EventoDeDominio;

/// <summary>
/// Evento pivotal do Event Storming: a transição de rascunho para item comercializável.
/// </summary>
public sealed record JogoPublicado(Guid JogoId, string Titulo) : EventoDeDominio;

/// <summary>Jogo retirado do catálogo. Não afeta as bibliotecas de quem já o adquiriu.</summary>
public sealed record JogoDespublicado(Guid JogoId) : EventoDeDominio;

/// <summary>Promoção criada e válida para o período informado.</summary>
public sealed record PromocaoCriada(Guid PromocaoId,
    decimal Percentual,
    IReadOnlyCollection<Guid> JogosIds) : EventoDeDominio;

/// <summary>Promoção encerrada. Dispara a restauração do preço base dos jogos abrangidos.</summary>
public sealed record PromocaoEncerrada(Guid PromocaoId) : EventoDeDominio;
