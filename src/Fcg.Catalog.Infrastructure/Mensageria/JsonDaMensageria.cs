using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Preserva um único formato de contrato para todos os publicadores e consumidores.</summary>
public static class JsonDaMensageria
{
    /// <summary>Formato JSON usado igualmente na gravação da outbox e na leitura das entregas.</summary>
    public static JsonSerializerOptions Opcoes { get; } = Criar();

    private static JsonSerializerOptions Criar()
    {
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opcoes.Converters.Add(new JsonStringEnumConverter());
        opcoes.MakeReadOnly(populateMissingResolver: true);
        return opcoes;
    }
}
