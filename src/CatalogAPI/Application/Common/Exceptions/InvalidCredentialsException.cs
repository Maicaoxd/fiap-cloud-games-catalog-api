namespace CatalogAPI.Application.Common.Exceptions
{
    public sealed class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
            : base(ApplicationMessages.Authentication.InvalidCredentials)
        {
        }
    }
}

