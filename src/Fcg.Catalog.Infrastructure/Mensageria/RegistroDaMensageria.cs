using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Registra a base sem EF; Notifications copia somente esta parte e o consumidor.</summary>
public static class RegistroDaMensageria
{
    /// <summary>Registra conexão, topologia e correlação; não exige banco de dados.</summary>
    public static IServiceCollection AdicionarMensageria(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        servicos.Configure<OpcoesDoRabbitMq>(configuracao.GetSection(OpcoesDoRabbitMq.Secao));
        servicos.TryAddSingleton<ConexaoRabbitMq>();
        servicos.TryAddSingleton<TopologiaRabbitMq>();
        servicos.TryAddScoped<ContextoDeCorrelacao>();
        return servicos;
    }

    /// <summary>Inicia o consumidor concreto como trabalhador do host.</summary>
    public static IServiceCollection AdicionarConsumidor<TConsumidor>(this IServiceCollection servicos)
        where TConsumidor : BackgroundService
    {
        servicos.AddHostedService<TConsumidor>();
        return servicos;
    }
}
