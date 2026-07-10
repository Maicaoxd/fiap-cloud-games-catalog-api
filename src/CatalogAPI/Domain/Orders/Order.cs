using CatalogAPI.Domain.Shared;

namespace CatalogAPI.Domain.Orders
{
    public sealed class Order : Entity
    {
        private readonly List<OrderItem> _items = [];

        public Guid UserId { get; private set; }
        public decimal TotalPrice { get; private set; }
        public PaymentStatus Status { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private Order()
        {
        }

        private Order(Guid userId, IEnumerable<OrderItem> items)
            : base(userId)
        {
            UserId = userId;
            _items.AddRange(items);
            TotalPrice = _items.Sum(item => item.Price);
            Status = PaymentStatus.Pending;
        }

        public static Order Place(Guid userId, IEnumerable<OrderItem> items)
        {
            EnsureUserIdIsRequired(userId);
            var itemList = items?.ToList() ?? [];
            EnsureItemsAreRequired(itemList);
            EnsureItemsAreUnique(itemList);

            return new Order(userId, itemList);
        }

        public void MarkPaymentProcessed(PaymentStatus status)
        {
            EnsureFinalStatus(status);

            Status = status;
            ProcessedAt = DateTime.UtcNow;
            MarkAsUpdated(UserId);
        }

        private static void EnsureUserIdIsRequired(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException(DomainMessages.Order.UserIdRequired);
        }

        private static void EnsureItemsAreRequired(IReadOnlyCollection<OrderItem> items)
        {
            if (items.Count == 0)
                throw new ArgumentException(DomainMessages.Order.ItemsRequired);
        }

        private static void EnsureItemsAreUnique(IEnumerable<OrderItem> items)
        {
            var hasDuplicates = items
                .GroupBy(item => item.GameId)
                .Any(group => group.Count() > 1);

            if (hasDuplicates)
                throw new ArgumentException(DomainMessages.Order.DuplicateItemsNotAllowed);
        }

        private static void EnsureFinalStatus(PaymentStatus status)
        {
            if (status is not PaymentStatus.Approved and not PaymentStatus.Rejected)
                throw new ArgumentException(DomainMessages.Order.FinalPaymentStatusRequired);
        }
    }
}
