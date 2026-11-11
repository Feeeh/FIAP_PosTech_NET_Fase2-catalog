using Fcg.Catalog.Application.Abstracoes;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Application.Catalogo;

/// <summary>Dados comerciais recebidos para cadastrar ou atualizar o jogo.</summary>
public sealed record DadosDoJogoComando(string Titulo, string Descricao, decimal Preco, string Genero);

/// <summary>Dados do jogo após a alteração, incluindo o estado de publicação.</summary>
/// <summary>Dados do jogo após a alteração, incluindo o estado de publicação.</summary>
public sealed record JogoResposta(Guid JogoId,
    string Titulo,
    string Descricao,
    decimal Preco,
    string Genero,
    string Status);

/// <summary>RF05 — cadastra um jogo. Ele nasce em Rascunho e só entra no catálogo ao ser publicado.</summary>
public sealed class CadastrarJogo(
    IRepositorioDeJogos repositorioDeJogos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<JogoResposta> ExecutarAsync(DadosDoJogoComando comando,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var titulo = Titulo.Criar(comando.Titulo);

        if (await repositorioDeJogos.ExisteComTituloAsync(titulo, null, cancellationToken))
        {
            throw new ExcecaoDeConflito("Já existe um jogo com este título no catálogo.");
        }

        var jogo = Jogo.Cadastrar(
            titulo,
            Descricao.Criar(comando.Descricao),
            Dinheiro.Criar(comando.Preco),
            Genero.Criar(comando.Genero),
            relogio.GetUtcNow());

        await repositorioDeJogos.AdicionarAsync(jogo, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(jogo);
    }

    internal static JogoResposta Mapear(Jogo jogo) => new(
        jogo.Id,
        jogo.Titulo.Valor,
        jogo.Descricao.Valor,
        jogo.Preco.Valor,
        jogo.Genero.Valor,
        jogo.Status.ToString());
}

/// <summary>RF05 — atualiza os dados comerciais de um jogo.</summary>
public sealed class AtualizarJogo(
    IRepositorioDeJogos repositorioDeJogos,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<JogoResposta> ExecutarAsync(
        Guid jogoId,
        DadosDoJogoComando comando,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var jogo = await repositorioDeJogos.ObterPorIdAsync(jogoId, cancellationToken)
            ?? throw new ExcecaoDeNaoEncontrado("Jogo não encontrado.");

        var titulo = Titulo.Criar(comando.Titulo);

        // Ignora o próprio jogo na checagem: renomear para o mesmo título não é conflito.
        if (await repositorioDeJogos.ExisteComTituloAsync(titulo, jogoId, cancellationToken))
        {
            throw new ExcecaoDeConflito("Já existe outro jogo com este título no catálogo.");
        }

        jogo.Atualizar(
            titulo,
            Descricao.Criar(comando.Descricao),
            Dinheiro.Criar(comando.Preco),
            Genero.Criar(comando.Genero));

        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return CadastrarJogo.Mapear(jogo);
    }
}

/// <summary>RF05 — publica ou despublica um jogo, controlando o que aparece no catálogo.</summary>
public sealed class AlterarPublicacaoDoJogo(
    IRepositorioDeJogos repositorioDeJogos,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<JogoResposta> PublicarAsync(Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterAsync(jogoId, cancellationToken);
        jogo.Publicar();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return CadastrarJogo.Mapear(jogo);
    }

    public async Task<JogoResposta> DespublicarAsync(Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterAsync(jogoId, cancellationToken);
        jogo.Despublicar();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return CadastrarJogo.Mapear(jogo);
    }

    private async Task<Jogo> ObterAsync(Guid jogoId, CancellationToken cancellationToken) =>
        await repositorioDeJogos.ObterPorIdAsync(jogoId, cancellationToken)
        ?? throw new ExcecaoDeNaoEncontrado("Jogo não encontrado.");
}

/// <summary>Define o desconto, o período e os jogos abrangidos pela promoção.</summary>
public sealed record CriarPromocaoComando(
    decimal PercentualDesconto,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    IReadOnlyCollection<Guid> JogosIds);

/// <summary>Confirma a promoção criada ou encerrada sem expor o agregado.</summary>
public sealed record PromocaoResposta(
    Guid PromocaoId,
    decimal PercentualDesconto,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    IReadOnlyCollection<Guid> JogosIds,
    bool Encerrada);

/// <summary>
/// RF07 — cria uma promoção. Rejeita período inválido (garantido por Periodo) e
/// promoção sobreposta, decisão registrada no Event Storming de jogos e promoções.
/// </summary>
public sealed class CriarPromocao(
    IRepositorioDePromocoes repositorioDePromocoes,
    IRepositorioDeJogos repositorioDeJogos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<PromocaoResposta> ExecutarAsync(
        CriarPromocaoComando comando,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var promocao = Promocao.Criar(
            PercentualDesconto.Criar(comando.PercentualDesconto),
            Periodo.Criar(comando.Inicio, comando.Fim),
            comando.JogosIds ?? [],
            relogio.GetUtcNow());

        await GarantirQueOsJogosExistemAsync(promocao.JogosIds, cancellationToken);
        await GarantirQueNaoHaSobreposicaoAsync(promocao, cancellationToken);

        await repositorioDePromocoes.AdicionarAsync(promocao, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(promocao);
    }

    private async Task GarantirQueOsJogosExistemAsync(
        IReadOnlyCollection<Guid> jogosIds,
        CancellationToken cancellationToken)
    {
        foreach (var jogoId in jogosIds)
        {
            _ = await repositorioDeJogos.ObterPorIdAsync(jogoId, cancellationToken)
                ?? throw new ExcecaoDeNaoEncontrado($"Jogo {jogoId} não encontrado.");
        }
    }

    private async Task GarantirQueNaoHaSobreposicaoAsync(Promocao novaPromocao, CancellationToken cancellationToken)
    {
        var promocoesConcorrentes = await repositorioDePromocoes.ObterVigentesParaJogosAsync(
            novaPromocao.JogosIds,
            cancellationToken);

        if (promocoesConcorrentes.Any(novaPromocao.ConflitaCom))
        {
            throw new ExcecaoDeConflito(
                "Já existe uma promoção vigente para pelo menos um destes jogos no período informado.");
        }
    }

    internal static PromocaoResposta Mapear(Promocao promocao) => new(
        promocao.Id,
        promocao.PercentualDesconto.Valor,
        promocao.Periodo.Inicio,
        promocao.Periodo.Fim,
        promocao.JogosIds,
        promocao.Encerrada);
}

/// <summary>RF07 — encerra uma promoção, restaurando o preço base.</summary>
public sealed class EncerrarPromocao(
    IRepositorioDePromocoes repositorioDePromocoes,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<PromocaoResposta> ExecutarAsync(Guid promocaoId, CancellationToken cancellationToken = default)
    {
        var promocao = await repositorioDePromocoes.ObterPorIdAsync(promocaoId, cancellationToken)
            ?? throw new ExcecaoDeNaoEncontrado("Promoção não encontrada.");

        promocao.Encerrar();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return CriarPromocao.Mapear(promocao);
    }
}
