using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// A única conexão do serviço com o RabbitMQ, compartilhada por consumidores e publicador.
/// Conexões AMQP são caras; o que cada um abre é um canal leve dentro desta conexão.
/// </summary>
public sealed class ConexaoRabbitMq(
    IOptions<OpcoesDoRabbitMq> opcoes,
    ILogger<ConexaoRabbitMq> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _exclusao = new(1, 1);
    private IConnection? _conexao;

    /// <summary>Obtém a conexão compartilhada, esperando o broker iniciar na primeira chamada.</summary>
    public async Task<IConnection> ObterAsync(CancellationToken cancellationToken)
    {
        await _exclusao.WaitAsync(cancellationToken);

        try
        {
            if (_conexao is not null)
            {
                return _conexao;
            }

            _conexao = await ConectarAsync(cancellationToken);
            return _conexao;
        }
        finally
        {
            _exclusao.Release();
        }
    }

    /// <summary>
    /// No Kubernetes não existe "depends_on": o serviço pode subir antes do broker. Por isso
    /// tentamos de novo até o prazo de inicialização acabar; depois disso a exceção derruba o
    /// host e o contêiner é reiniciado.
    /// </summary>
    private async Task<IConnection> ConectarAsync(CancellationToken cancellationToken)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(LimitesDaMensageria.PrazoDeInicializacao);

        var fabrica = CriarFabrica(opcoes.Value);

        while (true)
        {
            try
            {
                return await fabrica.CreateConnectionAsync(prazo.Token);
            }
            catch (Exception excecao) when (!prazo.IsCancellationRequested)
            {
                logger.LogWarning(excecao, "RabbitMQ indisponível; nova tentativa de conexão em instantes.");
                await Task.Delay(LimitesDaMensageria.IntervaloDeConexao, prazo.Token);
            }
        }
    }

    /// <summary>
    /// A recuperação automática reabre conexão, canais e consumidores se o broker cair depois
    /// da inicialização. O nome da aplicação identifica a conexão no painel do RabbitMQ.
    /// </summary>
    private static ConnectionFactory CriarFabrica(OpcoesDoRabbitMq configuracao) => new()
    {
        HostName = configuracao.Host,
        Port = configuracao.Porta,
        UserName = configuracao.Usuario,
        Password = configuracao.Senha,
        ClientProvidedName = configuracao.NomeDaAplicacao,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };

    /// <summary>Fecha a conexão e libera o controle de acesso ao encerrar o host.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null)
        {
            await _conexao.DisposeAsync();
        }

        _exclusao.Dispose();
    }
}
