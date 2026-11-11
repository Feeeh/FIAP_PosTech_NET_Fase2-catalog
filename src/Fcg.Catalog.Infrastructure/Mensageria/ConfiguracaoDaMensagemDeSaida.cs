using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Infrastructure.Mensageria;

/// <summary>Define a tabela que guarda eventos pendentes até o RabbitMQ confirmar a publicação.</summary>
public sealed class ConfiguracaoDaMensagemDeSaida : IEntityTypeConfiguration<MensagemDeSaida>
{
    /// <summary>Configura a tabela da outbox e o índice usado para buscar mensagens pendentes.</summary>
    public void Configure(EntityTypeBuilder<MensagemDeSaida> builder)
    {
        builder.ToTable("MensagensDeSaida");
        builder.HasKey(mensagem => mensagem.Id);
        builder.Property(mensagem => mensagem.Tipo).HasMaxLength(200);
        builder.Property(mensagem => mensagem.RoutingKey).HasMaxLength(200);
        builder.Property(mensagem => mensagem.CorrelationId).HasMaxLength(200);
        builder.HasIndex(mensagem => new { mensagem.EnviadaEm, mensagem.CriadaEm });
    }
}
