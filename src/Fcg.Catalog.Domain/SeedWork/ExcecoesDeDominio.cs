namespace Fcg.Catalog.Domain.SeedWork;

/// <summary>
/// Base de todo erro previsto pelas regras de negócio. O middleware da API traduz
/// cada tipo abaixo em um status HTTP — por isso o domínio não conhece HTTP.
/// </summary>
public abstract class ExcecaoDeDominio(string mensagem) : Exception(mensagem);

/// <summary>Dado recebido viola uma regra de formato ou de faixa. Vira HTTP 400.</summary>
public sealed class ExcecaoDeValidacao(string mensagem) : ExcecaoDeDominio(mensagem);

/// <summary>Já existe algo que impede a operação (e-mail, título, aquisição). Vira HTTP 409.</summary>
public sealed class ExcecaoDeConflito(string mensagem) : ExcecaoDeDominio(mensagem);

/// <summary>O recurso pedido não existe. Vira HTTP 404.</summary>
public sealed class ExcecaoDeNaoEncontrado(string mensagem) : ExcecaoDeDominio(mensagem);

/// <summary>Credenciais inválidas ou usuário desativado. Vira HTTP 401.</summary>
public sealed class ExcecaoDeAutenticacao(string mensagem) : ExcecaoDeDominio(mensagem);

/// <summary>
/// A requisição está bem formada, mas o estado atual não permite a operação
/// (adquirir jogo despublicado, publicar jogo já publicado). Vira HTTP 422.
/// </summary>
public sealed class ExcecaoDeEstadoInvalido(string mensagem) : ExcecaoDeDominio(mensagem);
