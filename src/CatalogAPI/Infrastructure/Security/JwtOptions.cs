using Microsoft.Extensions.Configuration;

namespace CatalogAPI.Infrastructure.Security
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public JwtOptions(
            string issuer,
            string audience,
            string secret,
            int expirationMinutes)
        {
            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("Jwt issuer was not configured.");

            if (string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("Jwt audience was not configured.");

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Jwt secret was not configured.");

            if (expirationMinutes <= 0)
                throw new InvalidOperationException("Jwt expiration minutes must be greater than zero.");

            Issuer = issuer;
            Audience = audience;
            Secret = secret;
            ExpirationMinutes = expirationMinutes;
        }

        public string Issuer { get; }
        public string Audience { get; }
        public string Secret { get; }
        public int ExpirationMinutes { get; }

        public static JwtOptions Create(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);

            if (!int.TryParse(section["ExpirationMinutes"], out var expirationMinutes))
                throw new InvalidOperationException("Jwt expiration minutes was not configured.");

            return new JwtOptions(
                section["Issuer"] ?? string.Empty,
                section["Audience"] ?? string.Empty,
                section["Secret"] ?? string.Empty,
                expirationMinutes);
        }
    }
}

