namespace CatalogAPI.Application.Common.Exceptions
{
    public sealed class GameNotFoundException : Exception
    {
        public GameNotFoundException()
            : base(ApplicationMessages.Game.NotFound)
        {
        }
    }
}

