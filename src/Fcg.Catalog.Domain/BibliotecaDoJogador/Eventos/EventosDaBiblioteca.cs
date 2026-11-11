using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;

/// <summary>
/// Evento pivotal do Event Storming (DDD.md seção 9): marca a fronteira entre
/// o Catálogo e a Biblioteca. É o que as fases seguintes vão consumir para
/// saber quem pode entrar em uma partida.
/// </summary>
public sealed record JogoAdquirido(Guid UsuarioId, Guid JogoId, decimal PrecoPago) : EventoDeDominio;

/// <summary>
/// Resultado da política "sempre que Jogo Adquirido, incluir item na biblioteca
/// com o preço congelado".
/// </summary>
public sealed record ItemDeBibliotecaIncluido(Guid UsuarioId, Guid JogoId, string Titulo) : EventoDeDominio;
