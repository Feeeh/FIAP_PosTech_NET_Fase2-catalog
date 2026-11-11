using Fcg.Catalog.Application.BibliotecaDoJogador;
using Fcg.Catalog.Infrastructure.Mensageria.Contratos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Recebe decisões do Payments para concluir pedidos e conceder acesso ao jogo.</summary>
public sealed class ConsumidorDePagamentos(
    ConexaoRabbitMq conexao,
    TopologiaRabbitMq topologia,
    IServiceScopeFactory escopos,
    ILogger<ConsumidorDePagamentos> logger,
    IOptions<OpcoesDasFilas> opcoes)
    : ConsumidorRabbitMq<PaymentProcessedEvent>(conexao, topologia, escopos, logger)
{
    protected override string Fila => opcoes.Value.PaymentProcessed;

    protected override string RoutingKey => RotasDosEventos.PagamentoProcessado;

    protected override bool EhValido(PaymentProcessedEvent evento)
    {
        var identificadoresValidos = evento.EventId != Guid.Empty
            && evento.PaymentId != Guid.Empty
            && evento.OrderId != Guid.Empty
            && evento.UserId != Guid.Empty
            && evento.GameId != Guid.Empty;
        var dadosDaCompraValidos = evento.OccurredAt != default
            && evento.Price >= 0
            && !string.IsNullOrWhiteSpace(evento.UserEmail)
            && !string.IsNullOrWhiteSpace(evento.GameTitle);

        var statusConhecido = evento.Status is PaymentProcessedEvent.Approved or PaymentProcessedEvent.Rejected;
        var motivoInformadoQuandoRejeitado = evento.Status != PaymentProcessedEvent.Rejected
            || !string.IsNullOrWhiteSpace(evento.Reason);

        return identificadoresValidos
            && dadosDaCompraValidos
            && statusConhecido
            && motivoInformadoQuandoRejeitado;
    }

    protected override Task ProcessarAsync(
        PaymentProcessedEvent evento,
        IServiceProvider servicosDoEscopo,
        CancellationToken cancellationToken)
    {
        var comando = new ProcessarResultadoDoPagamentoComando(
            evento.OrderId,
            evento.Status == PaymentProcessedEvent.Approved,
            evento.Reason);
        var casoDeUso = servicosDoEscopo.GetRequiredService<ProcessarResultadoDoPagamento>();

        return casoDeUso.ExecutarAsync(comando, cancellationToken);
    }
}
