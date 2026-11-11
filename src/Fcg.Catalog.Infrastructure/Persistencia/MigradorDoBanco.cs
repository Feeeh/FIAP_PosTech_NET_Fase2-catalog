using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fcg.Catalog.Infrastructure.Persistencia;

/// <summary>
/// Aplica as migrations do EF Core na inicialização, para que <c>docker compose up</c> e
/// <c>kubectl apply</c> funcionem sem nenhum passo manual de banco.
/// <para>
/// O SQL Server pode demorar mais que o serviço para subir, então tentamos de novo até o prazo
/// acabar. Desde o EF Core 9 a migração usa uma trava no banco, então duas réplicas subindo
/// juntas não aplicam o mesmo esquema duas vezes.
/// </para>
/// </summary>
public static class MigradorDoBanco
{
    /// <summary>Prazo total para o SQL Server ficar disponível.</summary>
    private static readonly TimeSpan PrazoDeInicializacao = TimeSpan.FromSeconds(60);

    /// <summary>Pausa entre uma tentativa e outra enquanto o banco inicia.</summary>
    private static readonly TimeSpan IntervaloEntreTentativas = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Aplica as migrations pendentes. Chamado antes de iniciar os trabalhadores que usam as tabelas.
    /// </summary>
    public static async Task MigrarAsync<TContexto>(
        this IServiceProvider servicos,
        CancellationToken cancellationToken = default) where TContexto : DbContext
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(PrazoDeInicializacao);

        while (true)
        {
            await using var escopo = servicos.CreateAsyncScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<TContexto>();

            try
            {
                await contexto.Database.MigrateAsync(prazo.Token);
                return;
            }
            catch (Exception excecao) when (!prazo.IsCancellationRequested)
            {
                var logger = escopo.ServiceProvider.GetRequiredService<ILogger<TContexto>>();
                logger.LogWarning(excecao, "SQL Server indisponível; nova tentativa de migração em instantes.");

                await Task.Delay(IntervaloEntreTentativas, prazo.Token);
            }
        }
    }
}
