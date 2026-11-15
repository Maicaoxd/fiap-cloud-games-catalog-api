using CatalogAPI.Domain.Games;
using CatalogAPI.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogAPI.Infrastructure.Persistence.Configurations;

public sealed class GameSearchOutboxConfiguration : IEntityTypeConfiguration<GameSearchOutboxMessage>
{
    public void Configure(EntityTypeBuilder<GameSearchOutboxMessage> builder)
    {
        builder.ToTable("GameSearchOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).UseIdentityColumn();
        builder.Property(message => message.Payload).IsRequired();
        builder.HasOne<Game>().WithMany().HasForeignKey(message => message.GameId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt, message.LeaseUntil });
    }
}
