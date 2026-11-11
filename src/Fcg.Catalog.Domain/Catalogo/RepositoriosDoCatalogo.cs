namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>Contrato de persistência do agregado Jogo.</summary>
public interface IRepositorioDeJogos
{
    Task<Jogo?> ObterPorIdAsync(Guid jogoId, CancellationToken cancellationToken = default);

    /// <summary>Sustenta a invariante de título único, reforçada por índice no banco.</summary>
    Task<bool> ExisteComTituloAsync(Titulo titulo,
        Guid? ignorarJogoId = null,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default);
}

/// <summary>Contrato de persistência do agregado Promocao.</summary>
public interface IRepositorioDePromocoes
{
    Task<Promocao?> ObterPorIdAsync(Guid promocaoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promoções não encerradas que abrangem qualquer um dos jogos informados.
    /// É o insumo da checagem de sobreposição feita em <see cref="Promocao.ConflitaCom"/>.
    /// </summary>
    Task<IReadOnlyCollection<Promocao>> ObterVigentesParaJogosAsync(
        IEnumerable<Guid> jogosIds,
        CancellationToken cancellationToken = default);

    /// <summary>Promoção que vale para um jogo em um instante — usada no cálculo do preço vigente.</summary>
    Task<Promocao?> ObterVigenteParaJogoAsync(Guid jogoId,
        DateTimeOffset instante,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default);
}
