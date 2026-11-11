using System.Security.Claims;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.API.Controllers;

/// <summary>
/// Lê a identidade do usuário a partir das claims do JWT.
/// <para>
/// O Id do dono da biblioteca sempre vem do token, nunca da rota ou do corpo da
/// requisição — é o que impede um usuário de consultar ou alterar a biblioteca de outro.
/// </para>
/// </summary>
public static class UsuarioAutenticado
{
    /// <summary>Nome do perfil de administrador, usado nas políticas de autorização.</summary>
    public const string PerfilAdministrador = "Administrador";

    public static Guid ObterId(ClaimsPrincipal usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var identificador = usuario.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? usuario.FindFirstValue("sub");

        return Guid.TryParse(identificador, out var usuarioId)
            ? usuarioId
            : throw new ExcecaoDeAutenticacao("Token sem identificação de usuário válida.");
    }
    /// <summary>Obtém o endereço do token para a confirmação assíncrona da compra.</summary>
    public static string ObterEmail(ClaimsPrincipal usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        return usuario.FindFirstValue(ClaimTypes.Email)
            ?? usuario.FindFirstValue("email")
            ?? throw new ExcecaoDeAutenticacao("Token sem e-mail do usuário.");
    }
}
