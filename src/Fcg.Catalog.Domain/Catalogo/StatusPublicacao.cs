namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Estados possíveis de um jogo no catálogo. As transições válidas entre eles
/// são responsabilidade do agregado <see cref="Jogo"/>, não deste tipo.
/// </summary>
public enum StatusPublicacao
{
    /// <summary>Cadastrado, mas invisível para os usuários e não adquirível.</summary>
    Rascunho = 0,

    /// <summary>Visível no catálogo público e disponível para aquisição.</summary>
    Publicado = 1,

    /// <summary>Retirado do catálogo. Não afeta quem já o tem na biblioteca.</summary>
    Despublicado = 2
}
