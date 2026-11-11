using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Catalogo;

/// <summary>Título do jogo. É único no catálogo, o que é reforçado por índice no banco.</summary>
public sealed class Titulo : ObjetoDeValor
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 200;

    private Titulo()
    {
    }

    private Titulo(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    public static Titulo Criar(string? valor)
    {
        var tituloLimpo = valor?.Trim() ?? string.Empty;

        if (tituloLimpo.Length < TamanhoMinimo)
        {
            throw new ExcecaoDeValidacao($"O título precisa ter pelo menos {TamanhoMinimo} caracteres.");
        }

        if (tituloLimpo.Length > TamanhoMaximo)
        {
            throw new ExcecaoDeValidacao($"O título pode ter no máximo {TamanhoMaximo} caracteres.");
        }

        return new Titulo(tituloLimpo);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}

/// <summary>Descrição comercial do jogo.</summary>
public sealed class Descricao : ObjetoDeValor
{
    public const int TamanhoMaximo = 2000;

    private Descricao()
    {
    }

    private Descricao(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    public static Descricao Criar(string? valor)
    {
        var descricaoLimpa = valor?.Trim() ?? string.Empty;

        if (descricaoLimpa.Length == 0)
        {
            throw new ExcecaoDeValidacao("A descrição é obrigatória.");
        }

        if (descricaoLimpa.Length > TamanhoMaximo)
        {
            throw new ExcecaoDeValidacao($"A descrição pode ter no máximo {TamanhoMaximo} caracteres.");
        }

        return new Descricao(descricaoLimpa);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}

/// <summary>Gênero do jogo, usado para filtrar o catálogo.</summary>
public sealed class Genero : ObjetoDeValor
{
    public const int TamanhoMaximo = 60;

    private Genero()
    {
    }

    private Genero(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    public static Genero Criar(string? valor)
    {
        var generoLimpo = valor?.Trim() ?? string.Empty;

        if (generoLimpo.Length == 0)
        {
            throw new ExcecaoDeValidacao("O gênero é obrigatório.");
        }

        if (generoLimpo.Length > TamanhoMaximo)
        {
            throw new ExcecaoDeValidacao($"O gênero pode ter no máximo {TamanhoMaximo} caracteres.");
        }

        return new Genero(generoLimpo);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}
