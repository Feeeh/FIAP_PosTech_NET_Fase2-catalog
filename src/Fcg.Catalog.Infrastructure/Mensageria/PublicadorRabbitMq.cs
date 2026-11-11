using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Envia as linhas da outbox pelo canal do ciclo e espera a confirmação de cada mensagem.</summary>
public sealed class PublicadorRabbitMq(
    ConexaoRabbitMq conexao,
    TopologiaRabbitMq topologia,
    IOptions<OpcoesDoRabbitMq> opcoes)
{
    private const string TipoDeConteudo = "application/json";

    /// <summary>Abre um canal com confirmações e declara os exchanges uma vez para o ciclo inteiro.</summary>
    public async Task<IChannel> AbrirCanalAsync(CancellationToken cancellationToken)
    {
        var ligacao = await conexao.ObterAsync(cancellationToken);
        var configuracao = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        var canal = await ligacao.CreateChannelAsync(configuracao, cancellationToken);

        try
        {
            await topologia.DeclararExchangesAsync(canal, cancellationToken);
            return canal;
        }
        catch
        {
            await canal.DisposeAsync();
            throw;
        }
    }

    /// <summary>Publica uma linha no canal já aberto; só retorna depois da confirmação do broker.</summary>
    public async Task PublicarAsync(
        IChannel canal,
        MensagemDeSaida mensagem,
        CancellationToken cancellationToken)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(LimitesDaMensageria.PrazoDePublicacao);

        var propriedades = new BasicProperties
        {
            MessageId = mensagem.Id.ToString(),
            Type = mensagem.Tipo,
            CorrelationId = mensagem.CorrelationId,
            ContentType = TipoDeConteudo,
            Persistent = true,
            Timestamp = new AmqpTimestamp(mensagem.CriadaEm.ToUnixTimeSeconds()),
            AppId = opcoes.Value.NomeDaAplicacao
        };

        // Mandatory e confirms com tracking tornam return/nack uma exceção.
        // O dispatcher só marca a linha após este await; uma exceção mantém o envio pendente.
        await canal.BasicPublishAsync(
            opcoes.Value.Exchange,
            mensagem.RoutingKey,
            mandatory: true,
            propriedades,
            Encoding.UTF8.GetBytes(mensagem.Conteudo),
            prazo.Token);
    }
}
