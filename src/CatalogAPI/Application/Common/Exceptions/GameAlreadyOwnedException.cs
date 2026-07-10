namespace CatalogAPI.Application.Common.Exceptions
{
    public sealed class GameAlreadyOwnedException : Exception
    {
        public GameAlreadyOwnedException()
            : base(ApplicationMessages.Library.GameAlreadyOwned)
        {
        }
    }
}

