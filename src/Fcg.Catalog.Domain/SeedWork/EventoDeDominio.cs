namespace Fcg.Catalog.Domain.SeedWork;

/// <summary>
/// Fato relevante que já aconteceu no domínio — os eventos levantados no Event Storming.
/// O nome é sempre no passado ("JogoAdquirido"), porque um evento não pode ser desfeito.
/// </summary>
public abstract record EventoDeDominio
{
    /// <summary>Instante em que o fato aconteceu.</summary>
    public DateTimeOffset OcorridoEm { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Nome do evento, usado como discriminador no log de eventos.</summary>
    public string Tipo => GetType().Name;
}
