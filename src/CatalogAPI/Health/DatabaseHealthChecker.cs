using CatalogAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Health
{
    public interface IDatabaseHealthChecker
    {
        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    }

    public sealed class DatabaseHealthChecker : IDatabaseHealthChecker
    {
        private readonly CatalogDbContext _dbContext;

        public DatabaseHealthChecker(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Database.CanConnectAsync(cancellationToken);
        }
    }
}

