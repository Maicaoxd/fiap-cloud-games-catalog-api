using System.Text.RegularExpressions;

namespace CatalogAPI.Infrastructure.Search;

public sealed class ElasticsearchOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "";
    public string IndexName { get; set; } = "fcg-games-v1";
    public string ApiKey { get; set; } = "";

    public bool IsValid(bool development) => !Enabled ||
        (Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) &&
         endpoint.UserInfo.Length == 0 && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0 &&
         endpoint.AbsolutePath == "/" &&
         (endpoint.Scheme == "https" || (development && endpoint.Scheme == "http")) &&
         Regex.IsMatch(IndexName, "^[a-z][a-z0-9-]{0,62}$"));
}
