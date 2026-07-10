using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogAPI.Infrastructure.Persistence.Configurations
{
    public sealed class LibraryConfiguration : IEntityTypeConfiguration<Library>
    {
        public void Configure(EntityTypeBuilder<Library> builder)
        {
            builder.ToTable("Libraries");

            builder.HasKey(library => library.Id);

            builder.Property(library => library.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(library => library.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(library => library.GameId)
                .HasColumnName("GameId")
                .IsRequired();

            builder.HasIndex(library => new { library.UserId, library.GameId })
                .IsUnique()
                .HasDatabaseName("IX_Libraries_UserId_GameId");

            builder.HasOne<Game>()
                .WithMany()
                .HasForeignKey(library => library.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(library => library.IsActive)
                .HasColumnName("IsActive")
                .IsRequired();

            builder.Property(library => library.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(library => library.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            builder.Property(library => library.UpdatedAt)
                .HasColumnName("UpdatedAt");

            builder.Property(library => library.UpdatedBy)
                .HasColumnName("UpdatedBy");
        }
    }
}

