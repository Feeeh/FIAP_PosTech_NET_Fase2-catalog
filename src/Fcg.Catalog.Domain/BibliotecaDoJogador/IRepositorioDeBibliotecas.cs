namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>
/// Contrato de persistência do agregado Biblioteca. Declarado no domínio e
/// implementado na infraestrutura — é o que mantém o domínio sem EF Core.
/// </summary>
public interface IRepositorioDeBibliotecas
{
    /// <summary>Busca a biblioteca do usuário, ou null se ele ainda não adquiriu nada.</summary>
    Task<Biblioteca?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Biblioteca biblioteca, CancellationToken cancellationToken = default);
}
