using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CatalogAPI.Application.Common.Exceptions;

namespace CatalogAPI.Api.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetRequiredUserId(this ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userId, out var parsedUserId))
                throw new InvalidCredentialsException();

            return parsedUserId;
        }
    }
}

