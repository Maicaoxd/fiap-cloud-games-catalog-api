using CatalogAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Search;

public sealed class GameSearchOutboxStore(CatalogDbContext db)
{
    public async Task<GameSearchOutboxMessage?> ClaimAsync(CancellationToken cancellationToken)
    {
        var lease = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
;WITH candidate AS (
 SELECT TOP (1) * FROM [GameSearchOutbox] WITH (UPDLOCK, READPAST, ROWLOCK)
 WHERE [ProcessedAt] IS NULL AND [NextAttemptAt] <= SYSUTCDATETIME()
 AND ([LeaseUntil] IS NULL OR [LeaseUntil] < SYSUTCDATETIME())
 ORDER BY [Id]
)
UPDATE candidate SET [LeaseId] = {lease},
 [LeaseUntil] = DATEADD(SECOND, 60, SYSUTCDATETIME()), [Attempts] = [Attempts] + 1;", cancellationToken);
        return await db.GameSearchOutbox.AsNoTracking().SingleOrDefaultAsync(m => m.LeaseId == lease, cancellationToken);
    }

    public Task<int> CompleteAsync(GameSearchOutboxMessage message, CancellationToken cancellationToken) =>
        db.GameSearchOutbox.Where(m => m.Id == message.Id && m.LeaseId == message.LeaseId)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ProcessedAt, DateTime.UtcNow)
                .SetProperty(m => m.LeaseId, (Guid?)null).SetProperty(m => m.LeaseUntil, (DateTime?)null), cancellationToken);

    public Task<int> RetryAsync(GameSearchOutboxMessage message, CancellationToken cancellationToken)
    {
        var retryAt = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts, 6))));
        return db.GameSearchOutbox.Where(m => m.Id == message.Id && m.LeaseId == message.LeaseId)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.NextAttemptAt, retryAt)
                .SetProperty(m => m.LeaseId, (Guid?)null).SetProperty(m => m.LeaseUntil, (DateTime?)null), cancellationToken);
    }
}
