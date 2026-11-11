using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// Trabalhador em segundo plano que envia ao RabbitMQ os eventos gravados na outbox.
/// <para>
/// A cada ciclo lê as linhas ainda não enviadas, publica uma por uma e marca como enviada só
/// a que o broker confirmou. Uma falha deixa aquela linha pendente para o próximo ciclo, sem
/// travar as demais. Cada ciclo usa um escopo novo, então o DbContext não vive entre ciclos.
/// </para>
/// </summary>
public sealed class DespachanteDaCaixaDeSaida<TContexto>(
    IServiceScopeFactory escopos,
    PublicadorRabbitMq publicador,
    TimeProvider relogio,
    ILogger<DespachanteDaCaixaDeSaida<TContexto>> logger)
    : BackgroundService where TContexto : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DespacharAsync(stoppingToken);
            }
            catch (Exception excecao) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    excecao,
                    "Falha na outbox; mensagens continuam pendentes para o próximo ciclo");
            }

            await Task.Delay(LimitesDaMensageria.IntervaloDoDespachante, stoppingToken);
        }
    }

    private async Task DespacharAsync(CancellationToken cancellationToken)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<TContexto>();
        var mensagens = await contexto.Set<MensagemDeSaida>()
            .Where(mensagem => mensagem.EnviadaEm == null)
            .OrderBy(mensagem => mensagem.CriadaEm)
            .Take(LimitesDaMensageria.TamanhoDoLote)
            .ToListAsync(cancellationToken);

        if (mensagens.Count == 0)
        {
            return;
        }

        await using var canal = await publicador.AbrirCanalAsync(cancellationToken);

        foreach (var mensagem in mensagens)
        {
            try
            {
                await publicador.PublicarAsync(canal, mensagem, cancellationToken);
            }
            catch (Exception excecao) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(excecao, "Envio de {MensagemId} falhou; continua pendente", mensagem.Id);
                continue;
            }

            mensagem.MarcarComoEnviada(relogio.GetUtcNow());
            await contexto.SaveChangesAsync(cancellationToken);
        }
    }
}
