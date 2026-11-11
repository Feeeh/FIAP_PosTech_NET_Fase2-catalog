namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// Configuração preenchida no startup e usada para conectar ao broker e declarar os exchanges.
/// </summary>
public sealed class OpcoesDoRabbitMq
{
    /// <summary>Seção lida para configurar a conexão e os exchanges do RabbitMQ.</summary>
    public const string Secao = "RabbitMq";

    /// <summary>Nome do serviço exibido na conexão e enviado no AppId das mensagens.</summary>
    public string NomeDaAplicacao { get; set; } = string.Empty;

    /// <summary>Endereço do broker acessível pelo serviço.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Porta AMQP do broker.</summary>
    public int Porta { get; set; } = 5672;

    /// <summary>Usuário do broker fornecido por Secret ou user-secrets.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Senha do broker fornecida por Secret ou user-secrets.</summary>
    public string Senha { get; set; } = string.Empty;

    /// <summary>Exchange de eventos compartilhado pelos serviços.</summary>
    public string Exchange { get; set; } = "fcg.events";

    /// <summary>Exchange que encaminha falhas definitivas às DLQs.</summary>
    public string ExchangeDeMensagensMortas { get; set; } = "fcg.events.dlx";
}
