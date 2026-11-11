using System.Reflection;
using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.BibliotecaDoJogador.Eventos;
using Fcg.Catalog.Domain.Compartilhado;
using Fcg.Catalog.Domain.SeedWork;

namespace Fcg.Catalog.Domain.Tests.BibliotecaDoJogador;

/// <summary>
/// Módulo escolhido para TDD (RNF07). É o Core Domain, e estas são as invariantes
/// descritas na seção 12.1 do DDD.md. Os testes foram escritos antes de Biblioteca.Adquirir.
/// </summary>
public class BibliotecaTestes
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JogoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OutroJogoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Agora = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    private static Biblioteca CriarBibliotecaVazia() => Biblioteca.Criar(UsuarioId);

    [Fact]
    public void Adquirir_incluir_o_jogo_na_biblioteca()
    {
        var biblioteca = CriarBibliotecaVazia();

        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);

        Assert.True(biblioteca.Possui(JogoId));
        var item = Assert.Single(biblioteca.Itens);
        Assert.Equal(JogoId, item.JogoId);
        Assert.Equal("Hollow Knight", item.TituloNoMomentoDaCompra);
    }

    [Fact]
    public void Adquirir_o_mesmo_jogo_duas_vezes_e_rejeitado()
    {
        var biblioteca = CriarBibliotecaVazia();
        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);

        Assert.Throws<ExcecaoDeConflito>(
            () => biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora));
    }

    [Fact]
    public void Aquisicao_duplicada_deixa_a_biblioteca_inalterada()
    {
        var biblioteca = CriarBibliotecaVazia();
        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);

        try
        {
            biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(19.90m), Agora.AddDays(1));
        }
        catch (ExcecaoDeConflito)
        {
            // esperado — o que importa é o estado depois da rejeição
        }

        var item = Assert.Single(biblioteca.Itens);
        Assert.Equal(49.90m, item.PrecoPago.Valor);
        Assert.Equal(Agora, item.DataAquisicao);
    }

    [Fact]
    public void Jogos_diferentes_convivem_na_mesma_biblioteca()
    {
        var biblioteca = CriarBibliotecaVazia();

        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);
        biblioteca.Adquirir(OutroJogoId, "Celeste", Dinheiro.Criar(37.00m), Agora);

        Assert.Equal(2, biblioteca.Itens.Count);
    }

    [Fact]
    public void Preco_pago_e_titulo_sao_congelados_no_instante_da_aquisicao()
    {
        var biblioteca = CriarBibliotecaVazia();
        var precoVigenteNaCompra = Dinheiro.Criar(24.95m);

        biblioteca.Adquirir(JogoId, "Hollow Knight", precoVigenteNaCompra, Agora);

        var item = Assert.Single(biblioteca.Itens);
        Assert.Equal(24.95m, item.PrecoPago.Valor);
        Assert.Equal("Hollow Knight", item.TituloNoMomentoDaCompra);
        Assert.Equal(Agora, item.DataAquisicao);
    }

    [Fact]
    public void Adquirir_levanta_os_eventos_do_event_storming()
    {
        var biblioteca = CriarBibliotecaVazia();

        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);

        Assert.Contains(biblioteca.EventosDeDominio, evento => evento is JogoAdquirido);
        Assert.Contains(biblioteca.EventosDeDominio, evento => evento is ItemDeBibliotecaIncluido);
    }

    [Fact]
    public void A_identidade_da_biblioteca_e_a_do_dono()
    {
        var biblioteca = CriarBibliotecaVazia();

        Assert.Equal(UsuarioId, biblioteca.UsuarioId);
        Assert.Equal(UsuarioId, biblioteca.Id);
    }

    [Fact]
    public void Itens_nao_pode_ser_alterado_por_fora_do_agregado()
    {
        var biblioteca = CriarBibliotecaVazia();
        biblioteca.Adquirir(JogoId, "Hollow Knight", Dinheiro.Criar(49.90m), Agora);

        // A coleção exposta rejeita qualquer mutação vinda de fora do agregado.
        var tentativaDeMutacao = (ICollection<ItemBiblioteca>)biblioteca.Itens;

        Assert.Throws<NotSupportedException>(() => tentativaDeMutacao.Clear());
        Assert.Throws<NotSupportedException>(() => tentativaDeMutacao.Remove(biblioteca.Itens.First()));
        Assert.Single(biblioteca.Itens);
    }

    [Fact]
    public void Nada_fora_do_agregado_consegue_instanciar_um_item()
    {
        var construtoresPublicos = typeof(ItemBiblioteca)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(construtoresPublicos);
    }

    [Fact]
    public void Um_item_incluido_nao_pode_ser_alterado()
    {
        var propriedadesComSetterPublico = typeof(ItemBiblioteca)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propriedade => propriedade.SetMethod is { IsPublic: true })
            .Select(propriedade => propriedade.Name);

        Assert.Empty(propriedadesComSetterPublico);
    }
}
