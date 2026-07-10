using CatalogAPI.Domain.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogAPI.Infrastructure.Persistence.Configurations
{
    public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable("Games", table =>
                table.HasCheckConstraint(
                    "CK_Games_Price_NotNegative",
                    "[Price] >= 0"));

            builder.HasKey(game => game.Id);

            builder.Property(game => game.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(game => game.Title)
                .HasColumnName("Title")
                .HasMaxLength(150)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS")
                .IsRequired();

            builder.HasIndex(game => game.Title)
                .IsUnique()
                .HasDatabaseName("IX_Games_Title");

            builder.Property(game => game.Description)
                .HasColumnName("Description")
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(game => game.Price)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(game => game.IsActive)
                .HasColumnName("IsActive")
                .IsRequired();

            builder.Property(game => game.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(game => game.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            builder.Property(game => game.UpdatedAt)
                .HasColumnName("UpdatedAt");

            builder.Property(game => game.UpdatedBy)
                .HasColumnName("UpdatedBy");
        }
    }
}

