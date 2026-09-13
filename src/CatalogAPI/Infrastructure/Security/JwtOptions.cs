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
                throw new InvalidOperationException("O emissor JWT não foi configurado.");

            if (string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("A audiência JWT não foi configurada.");

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("O segredo JWT não foi configurado.");

            if (expirationMinutes <= 0)
                throw new InvalidOperationException("O tempo de expiração JWT em minutos deve ser maior que zero.");

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
                throw new InvalidOperationException("O tempo de expiração JWT em minutos não foi configurado.");

            return new JwtOptions(
                section["Issuer"] ?? string.Empty,
                section["Audience"] ?? string.Empty,
                section["Secret"] ?? string.Empty,
                expirationMinutes);
        }
    }
}
