namespace CatalogAPI.Application.Common.Exceptions
{
    public sealed class InvalidPaymentStatusException : Exception
    {
        public InvalidPaymentStatusException()
            : base(ApplicationMessages.Payment.InvalidStatus)
        {
        }
    }
}

