using Fcg.Catalog.Domain.BibliotecaDoJogador;
using Fcg.Catalog.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Infrastructure.Persistencia.Configuracoes;

/// <summary>Reforça no banco a exclusividade da compra pendente, inclusive sob concorrência.</summary>
public sealed class ConfiguracaoDePedido : IEntityTypeConfiguration<Pedido>
{
    private const string PrefixoDoValor = "Valor";
    private const int TamanhoDoStatus = 20;
    private const string FiltroPendente = "[Status] = 'Pendente'";

    public void Configure(EntityTypeBuilder<Pedido> pedido)
    {
        pedido.ToTable("Pedidos");
        pedido.HasKey(entidade => entidade.Id);
        pedido.Property(entidade => entidade.UsuarioEmail)
            .HasMaxLength(Pedido.TamanhoMaximoDoEmail)
            .IsRequired();
        pedido.Property(entidade => entidade.TituloDoJogo)
            .HasMaxLength(Titulo.TamanhoMaximo)
            .IsRequired();
        pedido.Property(entidade => entidade.Status)
            .HasConversion<string>()
            .HasMaxLength(TamanhoDoStatus);
        pedido.Property(entidade => entidade.Motivo)
            .HasMaxLength(Pedido.TamanhoMaximoDoMotivo);
        pedido.OwnsOne(entidade => entidade.Valor, valor => ColunasDeDinheiro.Mapear(valor, PrefixoDoValor));
        pedido.HasIndex(entidade => new { entidade.UsuarioId, entidade.JogoId })
            .IsUnique()
            .HasFilter(FiltroPendente);
        pedido.Ignore(entidade => entidade.EventosDeDominio);
    }
}
