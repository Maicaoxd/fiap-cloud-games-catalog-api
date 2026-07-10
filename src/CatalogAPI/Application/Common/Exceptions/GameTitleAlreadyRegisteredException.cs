namespace CatalogAPI.Application.Common.Exceptions
{
    public sealed class GameTitleAlreadyRegisteredException : Exception
    {
        public GameTitleAlreadyRegisteredException()
            : base(ApplicationMessages.Game.TitleAlreadyRegistered)
        {
        }
    }
}

