namespace CatalogAPI.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string Configuration { get; set; } = "localhost:6379";
    public string? Password { get; set; }
}
