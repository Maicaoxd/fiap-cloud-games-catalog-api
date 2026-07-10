using CatalogAPI.Domain.Shared;

namespace CatalogAPI.Domain.Games
{
    public class Game : Entity
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }

        private Game()
        {
            Title = null!;
            Description = null!;
        }

        private Game(
            string title,
            string description,
            decimal price,
            Guid createdBy)
            : base(createdBy)
        {
            Title = SanitizeTitle(title);
            Description = SanitizeDescription(description);
            Price = price;
        }

        public static Game Create(
            string title,
            string description,
            decimal price,
            Guid createdBy)
        {
            EnsureTitleIsRequired(title);
            EnsureDescriptionIsRequired(description);
            EnsurePriceIsNotNegative(price);

            return new Game(title, description, price, createdBy);
        }

        public void Update(
            string title,
            string description,
            decimal price,
            Guid updatedBy)
        {
            EnsureTitleIsRequired(title);
            EnsureDescriptionIsRequired(description);
            EnsurePriceIsNotNegative(price);

            Title = SanitizeTitle(title);
            Description = SanitizeDescription(description);
            Price = price;
            MarkAsUpdated(updatedBy);
        }

        public void Activate(Guid activatedBy)
        {
            MarkAsActivated(activatedBy);
        }

        public void Deactivate(Guid deactivatedBy)
        {
            MarkAsDeactivated(deactivatedBy);
        }

        private static void EnsureTitleIsRequired(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException(DomainMessages.Game.TitleRequired);
        }

        private static void EnsureDescriptionIsRequired(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException(DomainMessages.Game.DescriptionRequired);
        }

        private static string SanitizeTitle(string title)
        {
            return title.Trim();
        }

        private static string SanitizeDescription(string description)
        {
            return description.Trim();
        }

        private static void EnsurePriceIsNotNegative(decimal price)
        {
            if (price < 0)
                throw new ArgumentException(DomainMessages.Game.PriceCannotBeNegative);
        }
    }
}

