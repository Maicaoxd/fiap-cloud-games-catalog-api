using CatalogAPI.Domain.Shared;

namespace CatalogAPI.Domain.Orders
{
    public sealed class OrderItem : Entity
    {
        public Guid OrderId { get; private set; }
        public Guid GameId { get; private set; }
        public decimal Price { get; private set; }

        private OrderItem()
        {
        }

        private OrderItem(Guid gameId, decimal price, Guid createdBy)
            : base(createdBy)
        {
            EnsureGameIdIsRequired(gameId);
            EnsurePriceIsNotNegative(price);

            GameId = gameId;
            Price = price;
        }

        public static OrderItem Create(Guid gameId, decimal price, Guid createdBy)
        {
            return new OrderItem(gameId, price, createdBy);
        }

        private static void EnsureGameIdIsRequired(Guid gameId)
        {
            if (gameId == Guid.Empty)
                throw new ArgumentException(DomainMessages.OrderItem.GameIdRequired);
        }

        private static void EnsurePriceIsNotNegative(decimal price)
        {
            if (price < 0)
                throw new ArgumentException(DomainMessages.OrderItem.PriceCannotBeNegative);
        }
    }
}
