namespace Fcg.Catalog.Domain.SeedWork;

/// <summary>
/// Entidade que é a porta de entrada de um agregado. Toda alteração dentro do agregado
/// passa por ela — é isso que permite garantir as invariantes em um único lugar.
/// </summary>
public abstract class RaizDeAgregado<TId> : Entidade<TId> where TId : notnull
{
    private readonly List<EventoDeDominio> _eventosDeDominio = [];

    protected RaizDeAgregado()
    {
    }

    protected RaizDeAgregado(TId id) : base(id)
    {
    }

    /// <summary>Eventos levantados desde a última limpeza, aguardando publicação.</summary>
    public IReadOnlyCollection<EventoDeDominio> EventosDeDominio => _eventosDeDominio.AsReadOnly();

    protected void RegistrarEvento(EventoDeDominio evento) => _eventosDeDominio.Add(evento);

    /// <summary>Chamado pela infraestrutura depois que os eventos já foram publicados.</summary>
    public void LimparEventosDeDominio() => _eventosDeDominio.Clear();
}
