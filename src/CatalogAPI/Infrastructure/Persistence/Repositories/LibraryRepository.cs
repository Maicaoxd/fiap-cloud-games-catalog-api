using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence.Repositories
{
    public sealed class LibraryRepository : ILibraryRepository
    {
        private const string UniqueOwnershipIndexName = "IX_Libraries_UserId_GameId";

        private readonly CatalogDbContext _dbContext;

        public LibraryRepository(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsByUserAndGameAsync(
            Guid userId,
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Libraries
                .AnyAsync(
                    library => library.UserId == userId && library.GameId == gameId,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<LibraryGameReadModel>> ListGamesByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Libraries
                .AsNoTracking()
                .Where(library => library.UserId == userId)
                .Join(
                    _dbContext.Games.AsNoTracking(),
                    library => library.GameId,
                    game => game.Id,
                    (library, game) => new
                    {
                        Library = library,
                        Game = game
                    })
                .OrderByDescending(result => result.Library.CreatedAt)
                .Select(result => new LibraryGameReadModel(
                    result.Library.Id,
                    result.Game.Id,
                    result.Game.Title,
                    result.Game.Description,
                    result.Game.Price,
                    result.Game.IsActive,
                    result.Library.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Library library, CancellationToken cancellationToken = default)
        {
            await _dbContext.Libraries.AddAsync(library, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                SqlServerUniqueConstraintDetector.IsUniqueConstraintViolation(
                    exception,
                    UniqueOwnershipIndexName))
            {
                throw new GameAlreadyOwnedException();
            }
        }
    }
}

