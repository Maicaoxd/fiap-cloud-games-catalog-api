using CatalogAPI.Domain.Orders;

namespace CatalogAPI.Application.Abstractions.Messaging
{
    public interface IOrderPlacedEventPublisher
    {
        Task PublishAsync(Order order, CancellationToken cancellationToken = default);
    }
}

