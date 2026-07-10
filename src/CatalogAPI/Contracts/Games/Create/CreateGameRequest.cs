using System.ComponentModel.DataAnnotations;
using CatalogAPI.Api.Common;

namespace CatalogAPI.Api.Contracts.Games.Create
{
    public sealed record CreateGameRequest(
        [Required(ErrorMessage = ApiMessages.Game.TitleRequired)]
        string Title,
        [Required(ErrorMessage = ApiMessages.Game.DescriptionRequired)]
        string Description,
        [Range(0, double.MaxValue, ErrorMessage = ApiMessages.Game.PriceCannotBeNegative)]
        decimal Price);
}

