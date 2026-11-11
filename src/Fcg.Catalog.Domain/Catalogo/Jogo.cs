using Fcg.Catalog.Domain.Catalogo.Eventos;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>
/// Título publicado no catálogo, com preço base e descrição.
/// <para>
/// Invariantes (DDD.md 12.1): preço não pode ser negativo (garantido por
/// <see cref="Dinheiro"/>); só um jogo Publicado aparece no catálogo e é adquirível;
/// título é único no catálogo.
/// </para>
/// </summary>
public sealed class Jogo : RaizDeAgregado<Guid>
{
    private Jogo()
    {
    }

    private Jogo(Guid id,
        Titulo titulo,
        Descricao descricao,
        Dinheiro preco,
        Genero genero,
        DateTimeOffset dataDeCadastro)
        : base(id)
    {
        Titulo = titulo;
        Descricao = descricao;
        Preco = preco;
        Genero = genero;
        Status = StatusPublicacao.Rascunho;
        DataDeCadastro = dataDeCadastro;
    }

    public Titulo Titulo { get; private set; } = null!;

    public Descricao Descricao { get; private set; } = null!;

    /// <summary>Preço cheio, sem promoção. O preço com desconto é calculado, nunca armazenado.</summary>
    public Dinheiro Preco { get; private set; } = null!;

    public Genero Genero { get; private set; } = null!;

    public StatusPublicacao Status { get; private set; }

    public DateTimeOffset DataDeCadastro { get; private set; }

    /// <summary>Só um jogo publicado pode ser adquirido — verificado no ato da aquisição.</summary>
    public bool EstaDisponivelParaAquisicao => Status == StatusPublicacao.Publicado;

    /// <summary>Cadastra o jogo. Nasce em Rascunho: publicar é um passo deliberado do administrador.</summary>
    public static Jogo Cadastrar(Titulo titulo,
        Descricao descricao,
        Dinheiro preco,
        Genero genero,
        DateTimeOffset dataDeCadastro)
    {
        ArgumentNullException.ThrowIfNull(titulo);
        ArgumentNullException.ThrowIfNull(descricao);
        ArgumentNullException.ThrowIfNull(preco);
        ArgumentNullException.ThrowIfNull(genero);

        var jogo = new Jogo(Guid.NewGuid(), titulo, descricao, preco, genero, dataDeCadastro);
        jogo.RegistrarEvento(new JogoCadastrado(jogo.Id, titulo.Valor) { OcorridoEm = dataDeCadastro });

        return jogo;
    }

    public void Atualizar(Titulo titulo, Descricao descricao, Dinheiro preco, Genero genero)
    {
        ArgumentNullException.ThrowIfNull(titulo);
        ArgumentNullException.ThrowIfNull(descricao);
        ArgumentNullException.ThrowIfNull(preco);
        ArgumentNullException.ThrowIfNull(genero);

        Titulo = titulo;
        Descricao = descricao;
        Preco = preco;
        Genero = genero;

        RegistrarEvento(new JogoAtualizado(Id, titulo.Valor));
    }

    /// <summary>Coloca o jogo no catálogo público.</summary>
    /// <exception cref="ExcecaoDeEstadoInvalido">Quando o jogo já está publicado.</exception>
    public void Publicar()
    {
        if (Status == StatusPublicacao.Publicado)
        {
            throw new ExcecaoDeEstadoInvalido("Este jogo já está publicado.");
        }

        Status = StatusPublicacao.Publicado;
        RegistrarEvento(new JogoPublicado(Id, Titulo.Valor));
    }

    /// <summary>Retira o jogo do catálogo, sem mexer em nenhuma biblioteca.</summary>
    /// <exception cref="ExcecaoDeEstadoInvalido">Quando o jogo não está publicado.</exception>
    public void Despublicar()
    {
        if (Status != StatusPublicacao.Publicado)
        {
            throw new ExcecaoDeEstadoInvalido("Só é possível despublicar um jogo que está publicado.");
        }

        Status = StatusPublicacao.Despublicado;
        RegistrarEvento(new JogoDespublicado(Id));
    }

    /// <summary>
    /// Barra a aquisição de um jogo que não está no catálogo — o caminho de exceção
    /// "Jogo despublicado" do Event Storming da aquisição.
    /// </summary>
    /// <exception cref="ExcecaoDeEstadoInvalido">Quando o jogo não está publicado.</exception>
    public void GarantirQuePodeSerAdquirido()
    {
        if (!EstaDisponivelParaAquisicao)
        {
            throw new ExcecaoDeEstadoInvalido("Este jogo não está disponível para aquisição.");
        }
    }
}
