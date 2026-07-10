using System.ComponentModel.DataAnnotations;

namespace CatalogAPI.Api.Contracts.Purchases
{
    public sealed record PurchaseGamesRequest(
        [Required]
        IReadOnlyCollection<Guid> GameIds);
}
