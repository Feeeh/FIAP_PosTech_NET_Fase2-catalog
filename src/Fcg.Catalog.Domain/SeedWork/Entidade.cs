namespace Fcg.Catalog.Domain.SeedWork;

/// <summary>
/// Base de toda Entidade: tem identidade própria e ciclo de vida.
/// Duas entidades são a mesma quando têm o mesmo <see cref="Id"/>,
/// mesmo que todos os outros atributos tenham mudado.
/// </summary>
public abstract class Entidade<TId> where TId : notnull
{
    /// <summary>Identificador que acompanha a entidade por toda a vida dela.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>Construtor sem parâmetros exigido pelo EF Core para materializar do banco.</summary>
    protected Entidade()
    {
    }

    protected Entidade(TId id) => Id = id;

    public override bool Equals(object? obj) =>
        obj is Entidade<TId> outra
        && outra.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(outra.Id, Id);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}
