using CatalogAPI.Application.Abstractions.Messaging;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Domain.Orders;

namespace CatalogAPI.Application.Purchases.PurchaseGame
{
    public sealed class PurchaseGameUseCase
    {
        private readonly IGameRepository _gameRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderPlacedEventPublisher _orderPlacedEventPublisher;

        public PurchaseGameUseCase(
            IGameRepository gameRepository,
            ILibraryRepository libraryRepository,
            IOrderRepository orderRepository,
            IOrderPlacedEventPublisher orderPlacedEventPublisher)
        {
            _gameRepository = gameRepository;
            _libraryRepository = libraryRepository;
            _orderRepository = orderRepository;
            _orderPlacedEventPublisher = orderPlacedEventPublisher;
        }

        public async Task<PurchaseGameResult> ExecuteAsync(
            PurchaseGameCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command.UserId == Guid.Empty)
                throw new InvalidCredentialsException();

            var requestedGameIds = command.GameIds
                .Where(gameId => gameId != Guid.Empty)
                .Distinct()
                .ToList();

            var games = await _gameRepository.ListByIdsAsync(requestedGameIds, cancellationToken);

            if (requestedGameIds.Count == 0 || games.Count != requestedGameIds.Count)
                throw new GameNotFoundException();

            if (games.Any(game => !game.IsActive))
                throw new GameUnavailableException();

            foreach (var gameId in requestedGameIds)
            {
                if (await _libraryRepository.ExistsByUserAndGameAsync(
                        command.UserId,
                        gameId,
                        cancellationToken))
                    throw new GameAlreadyOwnedException();
            }

            var items = games
                .Select(game => OrderItem.Create(game.Id, game.Price, command.UserId))
                .ToList();

            var order = Order.Place(command.UserId, items);

            await _orderRepository.AddAsync(order, cancellationToken);
            await _orderPlacedEventPublisher.PublishAsync(order, cancellationToken);

            return new PurchaseGameResult(order.Id);
        }
    }
}
