using System.Diagnostics.CodeAnalysis;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>
/// Guarda o CorrelationId da operação atual — a requisição HTTP ou a mensagem recebida — para
/// que os eventos gravados na outbox durante essa operação levem o mesmo identificador.
/// Preenchido pelo middleware HTTP ou pelo consumidor; lido pela UnidadeDeTrabalho.
/// </summary>
public sealed class ContextoDeCorrelacao
{
    /// <summary>Tamanho máximo aceito: cabe na coluna da outbox e mantém o log legível.</summary>
    public const int TamanhoMaximo = 64;

    /// <summary>Identificador da operação atual. Nasce com um valor novo até ser definido.</summary>
    public string CorrelationId { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Adota o identificador recebido de fora (cabeçalho X-Correlation-Id ou propriedade AMQP)
    /// quando ele é seguro; caso contrário gera um novo.
    /// <para>
    /// O valor vem do cliente, então é validado na fronteira: um cabeçalho gigante quebraria
    /// a gravação da outbox, e quebras de linha permitiriam forjar linhas de log.
    /// </para>
    /// </summary>
    /// <returns>O identificador adotado.</returns>
    public string Definir(string? recebido)
    {
        CorrelationId = EhSeguro(recebido) ? recebido : Guid.NewGuid().ToString();

        return CorrelationId;
    }

    /// <summary>Até 64 caracteres, só letras, dígitos e os separadores - _ . :</summary>
    private static bool EhSeguro([NotNullWhen(true)] string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > TamanhoMaximo)
        {
            return false;
        }

        return valor.All(caractere => char.IsAsciiLetterOrDigit(caractere) || caractere is '-' or '_' or '.' or ':');
    }
}
