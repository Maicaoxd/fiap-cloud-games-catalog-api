using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;

namespace CatalogAPI.Application.Libraries.List
{
    public sealed class ListLibraryGamesUseCase
    {
        private readonly ILibraryRepository _libraryRepository;

        public ListLibraryGamesUseCase(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<IReadOnlyCollection<ListLibraryGameResult>> ExecuteAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new InvalidCredentialsException();

            var libraryGames = await _libraryRepository.ListGamesByUserIdAsync(userId, cancellationToken);

            return libraryGames
                .Select(libraryGame => new ListLibraryGameResult(
                    libraryGame.LibraryId,
                    libraryGame.GameId,
                    libraryGame.Title,
                    libraryGame.Description,
                    libraryGame.Price,
                    libraryGame.IsActive,
                    libraryGame.AcquiredAt))
                .ToList();
        }
    }
}
