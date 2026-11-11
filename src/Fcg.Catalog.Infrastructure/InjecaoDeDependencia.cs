using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Application.BibliotecaDoJogador;
using Fcg.Catalog.Application.Catalogo;
using Fcg.Catalog.Application.Consultas;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Infrastructure.Consultas;
using Fcg.Catalog.Infrastructure.Mensageria;
using Fcg.Catalog.Infrastructure.Persistencia;
using Fcg.Catalog.Infrastructure.Repositorios;
using Fcg.Catalog.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Catalog.Infrastructure;

/// <summary>
/// Registra tudo que a API precisa para funcionar. Os lifetimes seguem o guia de
/// decisões: Scoped para o que acompanha a requisição, Singleton só para o que é
/// imutável e seguro para concorrência.
/// </summary>
public static class InjecaoDeDependencia
{
    public const string NomeDaConexaoSqlServer = "SqlServer";

    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        var connectionString = configuracao.GetConnectionString(NomeDaConexaoSqlServer)
            ?? throw new InvalidOperationException(
                $"A connection string '{NomeDaConexaoSqlServer}' não foi configurada.");

        servicos.AddDbContext<CatalogoDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

        // Relógio do sistema. Testes trocam por um RelogioFalso para controlar o "agora".
        servicos.AddSingleton(TimeProvider.System);

        // Imutável e sem estado por requisição: pode ser Singleton com segurança.
        servicos.AddSingleton<FabricaDeConexoes>(_ => new FabricaDeConexoes(connectionString));

        // Um por requisição: carregam o CorrelationId e o DbContext daquela requisição.
        servicos.Configure<OpcoesDasFilas>(configuracao.GetSection(OpcoesDasFilas.Secao));
        servicos.AdicionarMensageria(configuracao)
            .AdicionarPublicadorComOutbox<CatalogoDbContext>()
            .AdicionarConsumidor<ConsumidorDePagamentos>();
        servicos.AddScoped<IRepositorioDePedidos, RepositorioDePedidos>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        servicos.AddScoped<IRepositorioDeJogos, RepositorioDeJogos>();
        servicos.AddScoped<IRepositorioDePromocoes, RepositorioDePromocoes>();
        servicos.AddScoped<IRepositorioDeBibliotecas, RepositorioDeBibliotecas>();

        servicos.AddScoped<IConsultasDePedidos, ConsultasDePedidosDapper>();
        servicos.AddScoped<IConsultasDeCatalogo, ConsultasDeCatalogoDapper>();
        servicos.AddScoped<IConsultasDeBiblioteca, ConsultasDeBibliotecaDapper>();
        servicos.AddScoped<IConsultasDePromocoes, ConsultasDePromocoesDapper>();

        return servicos;
    }

    /// <summary>
    /// Registra os casos de uso. São Scoped porque dependem de repositórios Scoped —
    /// um Singleton aqui capturaria o DbContext da primeira requisição (o erro que o
    /// guia chama de "Scoped capturado por Singleton").
    /// </summary>
    public static IServiceCollection AdicionarCasosDeUso(this IServiceCollection servicos)
    {

        servicos.AddScoped<CadastrarJogo>();
        servicos.AddScoped<AtualizarJogo>();
        servicos.AddScoped<AlterarPublicacaoDoJogo>();
        servicos.AddScoped<CriarPromocao>();
        servicos.AddScoped<EncerrarPromocao>();

        servicos.AddScoped<RealizarPedido>();
        servicos.AddScoped<ProcessarResultadoDoPagamento>();

        return servicos;
    }
}
