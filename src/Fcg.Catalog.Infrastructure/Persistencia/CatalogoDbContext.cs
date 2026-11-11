using Fcg.Catalog.Infrastructure.Mensageria;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Infrastructure.Persistencia;

/// <summary>Guarda catálogo, promoções, biblioteca, pedidos e outbox no banco exclusivo do Catalog.</summary>
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<MensagemDeSaida> MensagensDeSaida => Set<MensagemDeSaida>();

    public DbSet<Jogo> Jogos => Set<Jogo>();

    public DbSet<Promocao> Promocoes => Set<Promocao>();

    public DbSet<Biblioteca> Bibliotecas => Set<Biblioteca>();

    /// <summary>
    /// Recolhe os eventos levantados pelos agregados desta transação e limpa a lista,
    /// para que a infraestrutura possa publicá-los depois do commit.
    /// </summary>
    public IReadOnlyCollection<EventoDeDominio> ColetarEventosDeDominio()
    {
        var raizesComEventos = ChangeTracker
            .Entries<RaizDeAgregado<Guid>>()
            .Select(entrada => entrada.Entity)
            .Where(raiz => raiz.EventosDeDominio.Count > 0)
            .ToList();

        var eventos = raizesComEventos.SelectMany(raiz => raiz.EventosDeDominio).ToList();

        foreach (var raiz in raizesComEventos)
        {
            raiz.LimparEventosDeDominio();
        }

        return eventos;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogoDbContext).Assembly);
    }
}
