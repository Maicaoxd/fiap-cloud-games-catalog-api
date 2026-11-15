using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Domain.Games;
using CatalogAPI.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence.Repositories
{
    public sealed class GameRepository : IGameRepository
    {
        private const string UniqueTitleIndexName = "IX_Games_Title";

        private readonly CatalogDbContext _dbContext;

        public GameRepository(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsByTitleAsync(
            string title,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            var sanitizedTitle = title.Trim();

            return await _dbContext.Games
                .AnyAsync(
                    game => game.Title == sanitizedTitle,
                    cancellationToken);
        }

        public async Task<bool> ExistsByTitleForAnotherGameAsync(
            string title,
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            var sanitizedTitle = title.Trim();

            return await _dbContext.Games
                .AnyAsync(
                    game => game.Id != gameId &&
                            game.Title == sanitizedTitle,
                    cancellationToken);
        }

        public async Task<Game?> GetByIdAsync(
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Games
                .SingleOrDefaultAsync(game => game.Id == gameId, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Game>> ListByIdsAsync(
            IReadOnlyCollection<Guid> gameIds,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Games
                .Where(game => gameIds.Contains(game.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Game>> ListActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Games
                .AsNoTracking()
                .Where(game => game.IsActive)
                .OrderBy(game => game.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
        {
            await _dbContext.Games.AddAsync(game, cancellationToken);

            try
            {
                await PersistWithSearchAsync(game, cancellationToken);
            }
            catch (DbUpdateException exception) when (
                SqlServerUniqueConstraintDetector.IsUniqueConstraintViolation(
                    exception,
                    UniqueTitleIndexName))
            {
                throw new GameTitleAlreadyRegisteredException();
            }
        }

        public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
        {
            _dbContext.Games.Update(game);

            try
            {
                await PersistWithSearchAsync(game, cancellationToken);
            }
            catch (DbUpdateException exception) when (
                SqlServerUniqueConstraintDetector.IsUniqueConstraintViolation(
                    exception,
                    UniqueTitleIndexName))
            {
                throw new GameTitleAlreadyRegisteredException();
            }
        }
        private async Task PersistWithSearchAsync(Game game, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            // A gravação do jogo mantém o lock até a criação da pendência na mesma transação.
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.GameSearchOutbox.Add(GameSearchOutboxMessage.From(game));
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
