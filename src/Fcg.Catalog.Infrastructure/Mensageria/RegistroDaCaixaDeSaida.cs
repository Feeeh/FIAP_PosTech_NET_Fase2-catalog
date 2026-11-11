using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Mantém o registro dependente de EF separado para permitir consumidores sem persistência.</summary>
public static class RegistroDaCaixaDeSaida
{
    /// <summary>Registra o publicador e o dispatcher que consulta a outbox deste DbContext.</summary>
    public static IServiceCollection AdicionarPublicadorComOutbox<TContexto>(this IServiceCollection servicos)
        where TContexto : DbContext
    {
        servicos.TryAddSingleton<PublicadorRabbitMq>();
        servicos.AddHostedService<DespachanteDaCaixaDeSaida<TContexto>>();
        return servicos;
    }
}
