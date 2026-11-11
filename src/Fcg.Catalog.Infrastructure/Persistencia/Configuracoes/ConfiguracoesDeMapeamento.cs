using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Fcg.Catalog.Domain.Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Constantes de coluna usadas em mais de um mapeamento, para que preço e preço pago
/// tenham exatamente a mesma precisão no banco.
/// </summary>
internal static class ColunasDeDinheiro
{
    internal const int PrecisaoDecimal = 18;
    internal const int EscalaDecimal = 2;
    internal const int TamanhoDaMoeda = 3;

    /// <summary>Mapeia o Objeto de Valor Dinheiro em duas colunas: valor e moeda.</summary>
    internal static void Mapear<TDono>(
        OwnedNavigationBuilder<TDono, Dinheiro> dinheiro,
        string nomeDaColunaDeValor)
        where TDono : class
    {
        dinheiro.Property(valor => valor.Valor)
            .HasColumnName(nomeDaColunaDeValor)
            .HasPrecision(PrecisaoDecimal, EscalaDecimal)
            .IsRequired();

        dinheiro.Property(valor => valor.Moeda)
            .HasColumnName($"{nomeDaColunaDeValor}Moeda")
            .HasMaxLength(TamanhoDaMoeda)
            .IsRequired();
    }
}

/// <summary>Mapeamento do agregado Jogo.</summary>
public sealed class ConfiguracaoDeJogo : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> jogo)
    {
        jogo.ToTable("Jogos");
        jogo.HasKey(entidade => entidade.Id);

        jogo.OwnsOne(entidade => entidade.Titulo, titulo =>
        {
            titulo.Property(valorDoTitulo => valorDoTitulo.Valor)
                .HasColumnName("Titulo")
                .HasMaxLength(Titulo.TamanhoMaximo)
                .IsRequired();

            // Invariante "título único no catálogo".
            titulo.HasIndex(valorDoTitulo => valorDoTitulo.Valor).IsUnique();
        });

        jogo.OwnsOne(entidade => entidade.Descricao, descricao =>
        {
            descricao.Property(valorDaDescricao => valorDaDescricao.Valor)
                .HasColumnName("Descricao")
                .HasMaxLength(Descricao.TamanhoMaximo)
                .IsRequired();
        });

        jogo.OwnsOne(entidade => entidade.Genero, genero =>
        {
            genero.Property(valorDoGenero => valorDoGenero.Valor)
                .HasColumnName("Genero")
                .HasMaxLength(Genero.TamanhoMaximo)
                .IsRequired();
        });

        jogo.OwnsOne(entidade => entidade.Preco, preco => ColunasDeDinheiro.Mapear(preco, "Preco"));

        jogo.Property(entidade => entidade.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        jogo.Property(entidade => entidade.DataDeCadastro).IsRequired();

        // O catálogo público filtra por status o tempo todo.
        jogo.HasIndex(entidade => entidade.Status);

        jogo.Ignore(entidade => entidade.EventosDeDominio);
    }
}

/// <summary>Mapeamento do agregado Promocao e da tabela de ligação com os jogos.</summary>
public sealed class ConfiguracaoDePromocao : IEntityTypeConfiguration<Promocao>
{
    public void Configure(EntityTypeBuilder<Promocao> promocao)
    {
        promocao.ToTable("Promocoes");
        promocao.HasKey(entidade => entidade.Id);

        promocao.OwnsOne(entidade => entidade.PercentualDesconto, percentual =>
        {
            percentual.Property(valorDoPercentual => valorDoPercentual.Valor)
                .HasColumnName("PercentualDesconto")
                .HasPrecision(5, 2)
                .IsRequired();
        });

        promocao.OwnsOne(entidade => entidade.Periodo, periodo =>
        {
            periodo.Property(valorDoPeriodo => valorDoPeriodo.Inicio).HasColumnName("Inicio").IsRequired();
            periodo.Property(valorDoPeriodo => valorDoPeriodo.Fim).HasColumnName("Fim").IsRequired();
        });

        // Tabela de ligação: guarda apenas Ids, porque Jogo é outro agregado.
        promocao.OwnsMany(entidade => entidade.Jogos, jogoEmPromocao =>
        {
            jogoEmPromocao.ToTable("JogosEmPromocao");
            jogoEmPromocao.WithOwner().HasForeignKey("PromocaoId");
            jogoEmPromocao.Property(referencia => referencia.JogoId).IsRequired();
            jogoEmPromocao.HasKey("PromocaoId", nameof(JogoEmPromocao.JogoId));
            jogoEmPromocao.HasIndex(referencia => referencia.JogoId);
        });

        promocao.Property(entidade => entidade.Encerrada).IsRequired();
        promocao.Property(entidade => entidade.DataDeCriacao).IsRequired();

        promocao.Ignore(entidade => entidade.JogosIds);
        promocao.Ignore(entidade => entidade.EventosDeDominio);
    }
}

/// <summary>Mapeamento do agregado Biblioteca e dos itens que vivem dentro dele.</summary>
public sealed class ConfiguracaoDeBiblioteca : IEntityTypeConfiguration<Biblioteca>
{
    public void Configure(EntityTypeBuilder<Biblioteca> biblioteca)
    {
        biblioteca.ToTable("Bibliotecas");

        // A identidade da biblioteca é a do dono, então a chave já garante uma por usuário.
        biblioteca.HasKey(entidade => entidade.Id);
        biblioteca.Property(entidade => entidade.Id).HasColumnName("UsuarioId").ValueGeneratedNever();

        biblioteca.OwnsMany(entidade => entidade.Itens, item =>
        {
            item.ToTable("ItensDeBiblioteca");
            item.WithOwner().HasForeignKey("BibliotecaId");
            item.HasKey(entidade => entidade.Id);

            item.Property(entidade => entidade.JogoId).IsRequired();
            item.Property(entidade => entidade.TituloNoMomentoDaCompra)
                .HasColumnName("TituloNoMomentoDaCompra")
                .HasMaxLength(Titulo.TamanhoMaximo)
                .IsRequired();
            item.Property(entidade => entidade.DataAquisicao).IsRequired();

            item.OwnsOne(entidade => entidade.PrecoPago, preco => ColunasDeDinheiro.Mapear(preco, "PrecoPago"));

            // Defesa em profundidade da invariante "o mesmo jogo nunca aparece duas vezes":
            // o agregado já barra, e o índice único cobre a condição de corrida
            // registrada no DDD.md seção 15 (duplo clique / retry).
            item.HasIndex("BibliotecaId", nameof(ItemBiblioteca.JogoId)).IsUnique();
        });

        biblioteca.Ignore(entidade => entidade.UsuarioId);
        biblioteca.Ignore(entidade => entidade.EventosDeDominio);
    }
}
