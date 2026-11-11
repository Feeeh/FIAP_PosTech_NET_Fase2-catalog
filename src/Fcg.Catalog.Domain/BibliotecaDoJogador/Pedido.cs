using System.Net.Mail;
using Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.BibliotecaDoJogador;

/// <summary>Pedido de compra que congela os dados enquanto o pagamento é decidido por outro serviço.</summary>
public sealed class Pedido : RaizDeAgregado<Guid>
{
    public const int TamanhoMaximoDoEmail = 254;
    public const int TamanhoMaximoDoMotivo = 1000;

    private Pedido()
    {
    }

    private Pedido(
        Guid id,
        Guid usuarioId,
        string usuarioEmail,
        Guid jogoId,
        string tituloDoJogo,
        Dinheiro valor,
        DateTimeOffset instante) : base(id)
    {
        UsuarioId = usuarioId;
        UsuarioEmail = usuarioEmail;
        JogoId = jogoId;
        TituloDoJogo = tituloDoJogo;
        // Sem promoção, o preço vigente é a própria instância do preço do Jogo: guardamos uma cópia.
        Valor = valor.Copiar();
        Status = StatusPedido.Pendente;
        CriadoEm = instante;
    }

    public Guid UsuarioId { get; private set; }

    /// <summary>Destinatário preservado para a confirmação da compra.</summary>
    public string UsuarioEmail { get; private set; } = string.Empty;

    public Guid JogoId { get; private set; }

    /// <summary>Título congelado para preservar o histórico da compra.</summary>
    public string TituloDoJogo { get; private set; } = string.Empty;

    /// <summary>Preço vigente na solicitação, independente de alterações no catálogo.</summary>
    public Dinheiro Valor { get; private set; } = null!;

    /// <summary>Decisão do pagamento; apenas pedidos pendentes podem ser concluídos.</summary>
    public StatusPedido Status { get; private set; }

    /// <summary>Justificativa recebida quando o pagamento é rejeitado.</summary>
    public string? Motivo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>Instante da decisão; ausente enquanto o pagamento estiver pendente.</summary>
    public DateTimeOffset? ConcluidoEm { get; private set; }

    /// <summary>Registra a solicitação com os dados congelados para o pagamento.</summary>
    public static Pedido Realizar(
        Guid usuarioId,
        string usuarioEmail,
        Guid jogoId,
        string tituloDoJogo,
        Dinheiro valor,
        DateTimeOffset instante)
    {
        ValidarDados(
            usuarioId,
            usuarioEmail,
            jogoId,
            tituloDoJogo,
            valor,
            instante);

        var pedido = new Pedido(
            Guid.NewGuid(),
            usuarioId,
            usuarioEmail.Trim(),
            jogoId,
            tituloDoJogo.Trim(),
            valor,
            instante);

        pedido.RegistrarEvento(new PedidoRealizado(
            pedido.Id,
            usuarioId,
            pedido.UsuarioEmail,
            jogoId,
            pedido.TituloDoJogo,
            valor.Valor)
        {
            OcorridoEm = instante
        });

        return pedido;
    }

    private static void ValidarDados(
        Guid usuarioId,
        string email,
        Guid jogoId,
        string titulo,
        Dinheiro? valor,
        DateTimeOffset instante)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeValidacao("O usuário do pedido é obrigatório.");
        }

        if (jogoId == Guid.Empty)
        {
            throw new ExcecaoDeValidacao("O jogo do pedido é obrigatório.");
        }

        if (valor is null)
        {
            throw new ExcecaoDeValidacao("O valor do pedido é obrigatório.");
        }

        if (instante == default)
        {
            throw new ExcecaoDeValidacao("O instante do pedido é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ExcecaoDeValidacao("O e-mail do usuário é obrigatório.");
        }

        if (email.Trim().Length > TamanhoMaximoDoEmail)
        {
            throw new ExcecaoDeValidacao("O e-mail do usuário excede o tamanho permitido.");
        }

        if (!MailAddress.TryCreate(email.Trim(), out _))
        {
            throw new ExcecaoDeValidacao("O e-mail do usuário é inválido.");
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ExcecaoDeValidacao("O título do jogo é obrigatório.");
        }
    }

    /// <summary>
    /// Aprova o pedido. Retorna false se já concluído: entrega duplicada, nada a fazer.
    /// </summary>
    public bool Aprovar(DateTimeOffset instante)
    {
        if (Status != StatusPedido.Pendente)
        {
            return false;
        }

        ValidarConclusao(instante);
        Status = StatusPedido.Aprovado;
        ConcluidoEm = instante;

        return true;
    }

    /// <summary>
    /// Rejeita o pedido. Retorna false se já concluído: entrega duplicada, nada a fazer.
    /// </summary>
    public bool Rejeitar(string motivo, DateTimeOffset instante)
    {
        if (Status != StatusPedido.Pendente)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ExcecaoDeValidacao("O motivo da rejeição é obrigatório.");
        }

        if (motivo.Trim().Length > TamanhoMaximoDoMotivo)
        {
            throw new ExcecaoDeValidacao("O motivo da rejeição excede o tamanho permitido.");
        }

        ValidarConclusao(instante);
        Status = StatusPedido.Rejeitado;
        Motivo = motivo.Trim();
        ConcluidoEm = instante;

        return true;
    }

    private void ValidarConclusao(DateTimeOffset instante)
    {
        if (instante < CriadoEm)
        {
            throw new ExcecaoDeValidacao("A conclusão não pode anteceder a criação do pedido.");
        }
    }
}

