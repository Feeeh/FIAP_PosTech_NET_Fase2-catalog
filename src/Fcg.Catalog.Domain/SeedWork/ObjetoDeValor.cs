namespace Fcg.Catalog.Domain.SeedWork;

/// <summary>
/// Base de todo Objeto de Valor: não tem identidade, é definido pelos próprios valores
/// e é imutável. Dois objetos com os mesmos componentes são intercambiáveis.
/// </summary>
public abstract class ObjetoDeValor
{
    /// <summary>Os valores que definem este objeto. É por eles que a igualdade é decidida.</summary>
    protected abstract IEnumerable<object?> ObterComponentesDeIgualdade();

    public override bool Equals(object? obj) =>
        obj is ObjetoDeValor outro
        && outro.GetType() == GetType()
        && outro.ObterComponentesDeIgualdade().SequenceEqual(ObterComponentesDeIgualdade());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var componente in ObterComponentesDeIgualdade())
        {
            hash.Add(componente);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(ObjetoDeValor? esquerda, ObjetoDeValor? direita) =>
        Equals(esquerda, direita);

    public static bool operator !=(ObjetoDeValor? esquerda, ObjetoDeValor? direita) =>
        !Equals(esquerda, direita);
}
