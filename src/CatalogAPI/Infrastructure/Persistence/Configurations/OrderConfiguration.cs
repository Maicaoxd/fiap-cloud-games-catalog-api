using CatalogAPI.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogAPI.Infrastructure.Persistence.Configurations
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders", table =>
                table.HasCheckConstraint(
                    "CK_Orders_TotalPrice_NotNegative",
                    "[TotalPrice] >= 0"));

            builder.HasKey(order => order.Id);

            builder.Property(order => order.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(order => order.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(order => order.TotalPrice)
                .HasColumnName("TotalPrice")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(order => order.Status)
                .HasColumnName("Status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(order => order.ProcessedAt)
                .HasColumnName("ProcessedAt");

            builder.Property(order => order.IsActive)
                .HasColumnName("IsActive")
                .IsRequired();

            builder.Property(order => order.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(order => order.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            builder.Property(order => order.UpdatedAt)
                .HasColumnName("UpdatedAt");

            builder.Property(order => order.UpdatedBy)
                .HasColumnName("UpdatedBy");

            builder.HasMany(order => order.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(order => order.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
