using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Infrastructure.Repositorios;

/// <summary>Repositório do agregado Jogo sobre EF Core.</summary>
public sealed class RepositorioDeJogos(CatalogoDbContext contexto) : IRepositorioDeJogos
{
    public Task<Jogo?> ObterPorIdAsync(Guid jogoId, CancellationToken cancellationToken = default) =>
        contexto.Jogos.FirstOrDefaultAsync(jogo => jogo.Id == jogoId, cancellationToken);

    public Task<bool> ExisteComTituloAsync(
        Titulo titulo,
        Guid? ignorarJogoId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(titulo);

        return contexto.Jogos.AnyAsync(
            jogo => jogo.Titulo.Valor == titulo.Valor && (ignorarJogoId == null || jogo.Id != ignorarJogoId),
            cancellationToken);
    }

    public async Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default) =>
        await contexto.Jogos.AddAsync(jogo, cancellationToken);
}

/// <summary>Repositório do agregado Promocao sobre EF Core.</summary>
public sealed class RepositorioDePromocoes(CatalogoDbContext contexto) : IRepositorioDePromocoes
{
    public Task<Promocao?> ObterPorIdAsync(Guid promocaoId, CancellationToken cancellationToken = default) =>
        contexto.Promocoes
            .Include(promocao => promocao.Jogos)
            .FirstOrDefaultAsync(promocao => promocao.Id == promocaoId, cancellationToken);

    public async Task<IReadOnlyCollection<Promocao>> ObterVigentesParaJogosAsync(
        IEnumerable<Guid> jogosIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jogosIds);

        var idsProcurados = jogosIds.Distinct().ToList();

        if (idsProcurados.Count == 0)
        {
            return [];
        }

        // Traz as candidatas em uma consulta só; a decisão de conflito é do domínio.
        return await contexto.Promocoes
            .Include(promocao => promocao.Jogos)
            .Where(promocao => !promocao.Encerrada
                && promocao.Jogos.Any(jogo => idsProcurados.Contains(jogo.JogoId)))
            .ToListAsync(cancellationToken);
    }

    public Task<Promocao?> ObterVigenteParaJogoAsync(
        Guid jogoId,
        DateTimeOffset instante,
        CancellationToken cancellationToken = default) =>
        contexto.Promocoes
            .Include(promocao => promocao.Jogos)
            .FirstOrDefaultAsync(
                promocao => !promocao.Encerrada
                    && promocao.Periodo.Inicio <= instante
                    && promocao.Periodo.Fim >= instante
                    && promocao.Jogos.Any(jogo => jogo.JogoId == jogoId),
                cancellationToken);

    public async Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default) =>
        await contexto.Promocoes.AddAsync(promocao, cancellationToken);
}

/// <summary>Repositório do agregado Biblioteca sobre EF Core.</summary>
public sealed class RepositorioDeBibliotecas(CatalogoDbContext contexto) : IRepositorioDeBibliotecas
{
    public Task<Biblioteca?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        contexto.Bibliotecas
            .Include(biblioteca => biblioteca.Itens)
            .FirstOrDefaultAsync(biblioteca => biblioteca.Id == usuarioId, cancellationToken);

    public async Task AdicionarAsync(Biblioteca biblioteca, CancellationToken cancellationToken = default) =>
        await contexto.Bibliotecas.AddAsync(biblioteca, cancellationToken);
}
