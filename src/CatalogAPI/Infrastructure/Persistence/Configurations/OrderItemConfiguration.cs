using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogAPI.Infrastructure.Persistence.Configurations
{
    public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems", table =>
                table.HasCheckConstraint(
                    "CK_OrderItems_Price_NotNegative",
                    "[Price] >= 0"));

            builder.HasKey(item => item.Id);

            builder.Property(item => item.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(item => item.OrderId)
                .HasColumnName("OrderId")
                .IsRequired();

            builder.Property(item => item.GameId)
                .HasColumnName("GameId")
                .IsRequired();

            builder.HasOne<Game>()
                .WithMany()
                .HasForeignKey(item => item.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(item => new { item.OrderId, item.GameId })
                .IsUnique()
                .HasDatabaseName("IX_OrderItems_OrderId_GameId");

            builder.Property(item => item.Price)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(item => item.IsActive)
                .HasColumnName("IsActive")
                .IsRequired();

            builder.Property(item => item.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(item => item.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            builder.Property(item => item.UpdatedAt)
                .HasColumnName("UpdatedAt");

            builder.Property(item => item.UpdatedBy)
                .HasColumnName("UpdatedBy");
        }
    }
}
