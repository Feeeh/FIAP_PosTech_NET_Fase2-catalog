using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.Catalogo.Eventos;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Tests.Catalogo;

/// <summary>Invariantes do Objeto de Valor Dinheiro.</summary>
public class DinheiroTestes
{
    [Fact]
    public void Rejeita_valor_negativo() =>
        Assert.Throws<ExcecaoDeValidacao>(() => Dinheiro.Criar(-0.01m));

    [Fact]
    public void Aceita_zero() =>
        Assert.Equal(0m, Dinheiro.Zero().Valor);

    [Fact]
    public void Aplicar_desconto_devolve_uma_nova_quantia_sem_alterar_a_original()
    {
        var precoBase = Dinheiro.Criar(100m);

        var precoComDesconto = precoBase.AplicarDescontoPercentual(25m);

        Assert.Equal(75m, precoComDesconto.Valor);
        Assert.Equal(100m, precoBase.Valor);
    }

    [Fact]
    public void Quantias_iguais_sao_intercambiaveis() =>
        Assert.Equal(Dinheiro.Criar(49.90m), Dinheiro.Criar(49.90m));

    [Fact]
    public void Rejeita_percentual_de_desconto_fora_de_0_a_100() =>
        Assert.Throws<ExcecaoDeValidacao>(() => Dinheiro.Criar(100m).AplicarDescontoPercentual(101m));
}

/// <summary>Invariantes de Periodo e PercentualDesconto.</summary>
public class ObjetosDeValorDePromocaoTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Periodo_rejeita_fim_anterior_ao_inicio() =>
        Assert.Throws<ExcecaoDeValidacao>(() => Periodo.Criar(Fim, Inicio));

    [Fact]
    public void Periodo_rejeita_inicio_igual_ao_fim() =>
        Assert.Throws<ExcecaoDeValidacao>(() => Periodo.Criar(Inicio, Inicio));

    [Fact]
    public void Periodo_reconhece_um_instante_dentro_dele()
    {
        var periodo = Periodo.Criar(Inicio, Fim);

        Assert.True(periodo.Contem(Inicio.AddDays(10)));
        Assert.False(periodo.Contem(Fim.AddDays(1)));
    }

    [Fact]
    public void Periodos_que_se_cruzam_sao_reconhecidos_como_sobrepostos()
    {
        var setembro = Periodo.Criar(Inicio, Fim);
        var meioDeSetembroAOutubro = Periodo.Criar(Inicio.AddDays(15), Fim.AddDays(15));
        var outubro = Periodo.Criar(Fim.AddDays(1), Fim.AddDays(30));

        Assert.True(setembro.SobrepoeCom(meioDeSetembroAOutubro));
        Assert.False(setembro.SobrepoeCom(outubro));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(91)]
    [InlineData(100)]
    public void PercentualDesconto_rejeita_valor_fora_da_faixa(decimal percentualInvalido) =>
        Assert.Throws<ExcecaoDeValidacao>(() => PercentualDesconto.Criar(percentualInvalido));

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(90)]
    public void PercentualDesconto_aceita_valor_dentro_da_faixa(decimal percentualValido) =>
        Assert.Equal(percentualValido, PercentualDesconto.Criar(percentualValido).Valor);
}

/// <summary>Invariantes do agregado Jogo.</summary>
public class JogoTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    internal static Jogo CriarJogoEmRascunho(string titulo = "Hollow Knight", decimal preco = 49.90m) =>
        Jogo.Cadastrar(
            Titulo.Criar(titulo),
            Descricao.Criar("Metroidvania desenhado à mão."),
            Dinheiro.Criar(preco),
            Genero.Criar("Aventura"),
            Agora);

    internal static Jogo CriarJogoPublicado(string titulo = "Hollow Knight", decimal preco = 49.90m)
    {
        var jogo = CriarJogoEmRascunho(titulo, preco);
        jogo.Publicar();
        return jogo;
    }

    [Fact]
    public void Jogo_nasce_em_rascunho_e_nao_e_adquirivel()
    {
        var jogo = CriarJogoEmRascunho();

        Assert.Equal(StatusPublicacao.Rascunho, jogo.Status);
        Assert.False(jogo.EstaDisponivelParaAquisicao);
        Assert.Throws<ExcecaoDeEstadoInvalido>(jogo.GarantirQuePodeSerAdquirido);
    }

    [Fact]
    public void Publicar_torna_o_jogo_adquirivel()
    {
        var jogo = CriarJogoPublicado();

        Assert.Equal(StatusPublicacao.Publicado, jogo.Status);
        Assert.True(jogo.EstaDisponivelParaAquisicao);
        Assert.Contains(jogo.EventosDeDominio, evento => evento is JogoPublicado);
    }

    [Fact]
    public void Publicar_um_jogo_ja_publicado_e_rejeitado()
    {
        var jogo = CriarJogoPublicado();

        Assert.Throws<ExcecaoDeEstadoInvalido>(jogo.Publicar);
    }

    [Fact]
    public void Despublicar_tira_o_jogo_do_catalogo()
    {
        var jogo = CriarJogoPublicado();

        jogo.Despublicar();

        Assert.Equal(StatusPublicacao.Despublicado, jogo.Status);
        Assert.Throws<ExcecaoDeEstadoInvalido>(jogo.GarantirQuePodeSerAdquirido);
    }

    [Fact]
    public void Despublicar_um_jogo_que_nao_esta_publicado_e_rejeitado()
    {
        var jogo = CriarJogoEmRascunho();

        Assert.Throws<ExcecaoDeEstadoInvalido>(jogo.Despublicar);
    }

    [Fact]
    public void Jogo_nao_pode_ter_preco_negativo() =>
        Assert.Throws<ExcecaoDeValidacao>(() => CriarJogoEmRascunho(preco: -1m));
}

/// <summary>Invariantes do agregado Promocao.</summary>
public class PromocaoTestes
{
    private static readonly Guid JogoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OutroJogoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    private static Promocao CriarPromocao(
        decimal percentual = 50m,
        DateTimeOffset? inicio = null,
        DateTimeOffset? fim = null,
        params Guid[] jogosIds) =>
        Promocao.Criar(
            PercentualDesconto.Criar(percentual),
            Periodo.Criar(inicio ?? Inicio, fim ?? Fim),
            jogosIds.Length == 0 ? [JogoId] : jogosIds,
            Inicio.AddDays(-1));

    [Fact]
    public void Promocao_precisa_abranger_ao_menos_um_jogo() =>
        Assert.Throws<ExcecaoDeValidacao>(() => Promocao.Criar(
            PercentualDesconto.Criar(50m),
            Periodo.Criar(Inicio, Fim),
            [],
            Inicio));

    [Fact]
    public void Promocao_esta_vigente_dentro_do_periodo()
    {
        var promocao = CriarPromocao();

        Assert.True(promocao.EstaVigenteEm(Inicio.AddDays(10)));
        Assert.False(promocao.EstaVigenteEm(Fim.AddDays(1)));
    }

    [Fact]
    public void Promocao_encerrada_deixa_de_estar_vigente()
    {
        var promocao = CriarPromocao();

        promocao.Encerrar();

        Assert.False(promocao.EstaVigenteEm(Inicio.AddDays(10)));
        Assert.Contains(promocao.EventosDeDominio, evento => evento is PromocaoEncerrada);
    }

    [Fact]
    public void Encerrar_duas_vezes_e_rejeitado()
    {
        var promocao = CriarPromocao();
        promocao.Encerrar();

        Assert.Throws<ExcecaoDeEstadoInvalido>(promocao.Encerrar);
    }

    [Fact]
    public void Duas_promocoes_no_mesmo_jogo_com_periodos_cruzados_conflitam()
    {
        var promocaoDeSetembro = CriarPromocao(jogosIds: [JogoId]);
        var promocaoDoMeioDeSetembro = CriarPromocao(
            inicio: Inicio.AddDays(15),
            fim: Fim.AddDays(15),
            jogosIds: [JogoId]);

        Assert.True(promocaoDeSetembro.ConflitaCom(promocaoDoMeioDeSetembro));
    }

    [Fact]
    public void Promocoes_em_jogos_diferentes_nao_conflitam()
    {
        var promocaoDeUmJogo = CriarPromocao(jogosIds: [JogoId]);
        var promocaoDeOutroJogo = CriarPromocao(jogosIds: [OutroJogoId]);

        Assert.False(promocaoDeUmJogo.ConflitaCom(promocaoDeOutroJogo));
    }

    [Fact]
    public void Promocoes_no_mesmo_jogo_em_periodos_separados_nao_conflitam()
    {
        var promocaoDeSetembro = CriarPromocao(jogosIds: [JogoId]);
        var promocaoDeOutubro = CriarPromocao(
            inicio: Fim.AddDays(1),
            fim: Fim.AddDays(30),
            jogosIds: [JogoId]);

        Assert.False(promocaoDeSetembro.ConflitaCom(promocaoDeOutubro));
    }

    [Fact]
    public void Promocao_encerrada_nao_conflita_com_nada()
    {
        var promocaoEncerrada = CriarPromocao(jogosIds: [JogoId]);
        promocaoEncerrada.Encerrar();
        var novaPromocao = CriarPromocao(jogosIds: [JogoId]);

        Assert.False(novaPromocao.ConflitaCom(promocaoEncerrada));
    }

    [Fact]
    public void Ids_repetidos_de_jogo_sao_guardados_uma_vez_so()
    {
        var promocao = CriarPromocao(jogosIds: [JogoId, JogoId, OutroJogoId]);

        Assert.Equal(2, promocao.JogosIds.Count);
    }
}

/// <summary>RF08 — o Serviço de Domínio que combina Jogo e Promoção.</summary>
public class CalculadoraDePrecoVigenteTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DentroDoPeriodo = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

    private static Promocao CriarPromocaoDe(decimal percentual, Guid jogoId) =>
        Promocao.Criar(
            PercentualDesconto.Criar(percentual),
            Periodo.Criar(Inicio, Fim),
            [jogoId],
            Inicio.AddDays(-1));

    [Fact]
    public void Sem_promocao_o_preco_vigente_e_o_preco_base()
    {
        var jogo = JogoTestes.CriarJogoPublicado(preco: 100m);

        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, null, DentroDoPeriodo);

        Assert.Equal(100m, precoVigente.Valor);
    }

    [Fact]
    public void Com_promocao_vigente_o_desconto_e_aplicado()
    {
        var jogo = JogoTestes.CriarJogoPublicado(preco: 100m);
        var promocao = CriarPromocaoDe(30m, jogo.Id);

        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, promocao, DentroDoPeriodo);

        Assert.Equal(70m, precoVigente.Valor);
    }

    [Fact]
    public void Fora_do_periodo_o_preco_volta_a_ser_o_base()
    {
        var jogo = JogoTestes.CriarJogoPublicado(preco: 100m);
        var promocao = CriarPromocaoDe(30m, jogo.Id);

        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, promocao, Fim.AddDays(1));

        Assert.Equal(100m, precoVigente.Valor);
    }

    [Fact]
    public void Promocao_que_nao_abrange_o_jogo_nao_muda_o_preco()
    {
        var jogo = JogoTestes.CriarJogoPublicado(preco: 100m);
        var promocaoDeOutroJogo = CriarPromocaoDe(30m, Guid.NewGuid());

        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, promocaoDeOutroJogo, DentroDoPeriodo);

        Assert.Equal(100m, precoVigente.Valor);
    }

    [Fact]
    public void Promocao_encerrada_nao_muda_o_preco()
    {
        var jogo = JogoTestes.CriarJogoPublicado(preco: 100m);
        var promocao = CriarPromocaoDe(30m, jogo.Id);
        promocao.Encerrar();

        var precoVigente = CalculadoraDePrecoVigente.Calcular(jogo, promocao, DentroDoPeriodo);

        Assert.Equal(100m, precoVigente.Valor);
    }
}
