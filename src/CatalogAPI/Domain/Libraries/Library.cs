using CatalogAPI.Domain.Shared;

namespace CatalogAPI.Domain.Libraries
{
    public sealed class Library : Entity
    {
        public Guid UserId { get; private set; }
        public Guid GameId { get; private set; }

        private Library()
        {
        }

        private Library(Guid userId, Guid gameId)
            : base(userId)
        {
            UserId = userId;
            GameId = gameId;
        }

        public static Library Create(Guid userId, Guid gameId)
        {
            EnsureUserIdIsRequired(userId);
            EnsureGameIdIsRequired(gameId);

            return new Library(userId, gameId);
        }

        private static void EnsureUserIdIsRequired(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException(DomainMessages.Library.UserIdRequired);
        }

        private static void EnsureGameIdIsRequired(Guid gameId)
        {
            if (gameId == Guid.Empty)
                throw new ArgumentException(DomainMessages.Library.GameIdRequired);
        }
    }
}

