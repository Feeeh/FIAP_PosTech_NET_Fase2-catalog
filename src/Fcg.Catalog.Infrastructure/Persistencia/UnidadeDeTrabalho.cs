using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Infrastructure.Mensageria;
using Fcg.Catalog.Domain.SeedWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Infrastructure.Persistencia;

/// <summary>Salva os agregados e os contratos de integração numa única transação do banco.</summary>
public sealed class UnidadeDeTrabalho(
    CatalogoDbContext contexto,
    ContextoDeCorrelacao correlacao) : IUnidadeDeTrabalho
{
    public async Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        var eventos = contexto.ColetarEventosDeDominio();

        foreach (var evento in eventos)
        {
            var mensagem = MapeadorDeEventosDeIntegracao.Mapear(evento, correlacao.CorrelationId);

            if (mensagem is not null)
            {
                contexto.MensagensDeSaida.Add(mensagem);
            }
        }

        try
        {
            return await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (
            excecao.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // O índice único cobre duas requisições que passaram juntas pela checagem prévia.
            throw new ExcecaoDeConflito(
                "Esta operação conflita com um registro que já existe. Tente novamente.");
        }
    }
}
