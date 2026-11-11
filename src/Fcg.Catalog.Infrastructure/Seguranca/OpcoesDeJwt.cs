using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Catalog.Infrastructure.Seguranca;

/// <summary>
/// Configuração do JWT. A chave nunca fica no appsettings: vem de user-secrets em
/// desenvolvimento e de variável de ambiente em execução (RNF05).
/// </summary>
public sealed class OpcoesDeJwt
{
    public const string Secao = "Jwt";

    /// <summary>Tamanho mínimo da chave para HS256 — 256 bits.</summary>
    public const int TamanhoMinimoDaChaveEmBytes = 32;

    public string ChaveSecreta { get; set; } = string.Empty;

    public string Emissor { get; set; } = "fcg-users-api";

    public string Audiencia { get; set; } = "fcg-clientes";

    /// <summary>
    /// Falha cedo e com mensagem clara se a chave não foi configurada ou é curta demais.
    /// Uma chave fraca torna a assinatura do token quebrável.
    /// </summary>
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(ChaveSecreta))
        {
            throw new InvalidOperationException(
                "A chave do JWT não foi configurada. "
                + "Defina Jwt:ChaveSecreta via user-secrets ou variável de ambiente.");
        }

        if (Encoding.UTF8.GetByteCount(ChaveSecreta) < TamanhoMinimoDaChaveEmBytes)
        {
            throw new InvalidOperationException(
                $"A chave do JWT precisa ter pelo menos {TamanhoMinimoDaChaveEmBytes} bytes.");
        }
    }

    public SymmetricSecurityKey ObterChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(ChaveSecreta));
}

