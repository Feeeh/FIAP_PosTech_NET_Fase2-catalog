namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Parâmetros operacionais comuns ao kit para limitar espera, memória e reentregas.</summary>
public static class LimitesDaMensageria
{
    /// <summary>Prazo total para o broker iniciar antes de encerrar o host.</summary>
    public static readonly TimeSpan PrazoDeInicializacao = TimeSpan.FromSeconds(60);

    /// <summary>Espera entre tentativas de abrir a conexão inicial.</summary>
    public static readonly TimeSpan IntervaloDeConexao = TimeSpan.FromSeconds(2);

    /// <summary>Espera entre consultas da outbox.</summary>
    public static readonly TimeSpan IntervaloDoDespachante = TimeSpan.FromSeconds(2);

    /// <summary>Espera antes de devolver uma entrega com falha para evitar um ciclo de erros sem pausa.</summary>
    public static readonly TimeSpan EsperaAntesDoNack = TimeSpan.FromSeconds(2);

    /// <summary>Tempo máximo para confirmar uma publicação.</summary>
    public static readonly TimeSpan PrazoDePublicacao = TimeSpan.FromSeconds(10);

    /// <summary>Entregas não confirmadas permitidas por consumidor.</summary>
    public const ushort Prefetch = 10;

    /// <summary>Máximo de pendências carregadas por ciclo.</summary>
    public const int TamanhoDoLote = 50;

    /// <summary>Reentregas permitidas antes de mover uma mensagem para a DLQ.</summary>
    public const int LimiteDeEntregas = 3;
}
