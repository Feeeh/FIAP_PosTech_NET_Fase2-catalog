namespace Fcg.Catalog.Application.Abstracoes;

/// <summary>Persiste agregados e eventos da operação na mesma transação.</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
