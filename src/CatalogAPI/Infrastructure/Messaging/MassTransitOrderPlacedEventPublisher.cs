using CatalogAPI.Application.Abstractions.Messaging;
using CatalogAPI.Domain.Orders;
using FiapCloudGames.Contracts.Events;
using MassTransit;

namespace CatalogAPI.Infrastructure.Messaging
{
    public sealed class MassTransitOrderPlacedEventPublisher : IOrderPlacedEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public MassTransitOrderPlacedEventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public Task PublishAsync(Order order, CancellationToken cancellationToken = default)
        {
            var games = order.Items
                .Select(item => new OrderPlacedGameEventItem(item.GameId, item.Price))
                .ToList();

            var @event = new OrderPlacedEvent(
                order.Id,
                order.UserId,
                games,
                order.TotalPrice,
                order.CreatedAt);

            return _publishEndpoint.Publish(@event, cancellationToken);
        }
    }
}
