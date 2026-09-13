namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DatabaseName { get; set; } = "FiapCloudGamesCatalog";
    public string CollectionName { get; set; } = "game_details";
    public int OperationTimeoutSeconds { get; set; } = 2;
}
