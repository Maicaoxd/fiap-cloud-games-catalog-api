using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Libraries;
using CatalogAPI.Domain.Orders;
using CatalogAPI.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence
{
    public sealed class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<GameSearchOutboxMessage> GameSearchOutbox => Set<GameSearchOutboxMessage>();
        public DbSet<Library> Libraries => Set<Library>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
