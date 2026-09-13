using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace CatalogAPI.Application.Games.Details;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GameDetailsContent
{
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string[]? Genres { get; init; } = [];
    public string[]? Platforms { get; init; } = [];
    public string[]? Languages { get; init; } = [];
    public string[]? Tags { get; init; } = [];
    public GameMedia? Media { get; init; }
    public Dictionary<string, PlatformRequirements>? SystemRequirements { get; init; } = [];
    public Dictionary<string, JsonElement>? Attributes { get; init; } = [];

    public GameDetailsContent NormalizeAndValidate()
    {
        Text(Developer, 150); Text(Publisher, 150);
        List(Genres, 30); List(Platforms, 30); List(Languages, 50); List(Tags, 50);
        Url(Media?.CoverUrl); Url(Media?.TrailerUrl);
        if (Media?.ScreenshotUrls is { } screenshots)
        {
            if (screenshots.Length > 20) throw new ArgumentException("No máximo 20 screenshots são permitidos.");
            foreach (var url in screenshots) Url(url, required: true);
        }
        if (SystemRequirements?.Count > 20) throw new ArgumentException("No máximo 20 plataformas são permitidas.");
        foreach (var (key, requirements) in SystemRequirements ?? [])
        {
            Key(key);
            if (requirements is null) throw new ArgumentException("Requisitos da plataforma não podem ser nulos.");
            Requirements(requirements.Minimum); Requirements(requirements.Recommended);
        }
        if (Attributes?.Count > 50) throw new ArgumentException("No máximo 50 atributos são permitidos.");
        foreach (var (key, value) in Attributes ?? [])
        {
            Key(key);
            if (value.ValueKind == JsonValueKind.Array)
            {
                if (value.GetArrayLength() > 20) throw new ArgumentException("Arrays de atributos permitem até 20 valores.");
                foreach (var item in value.EnumerateArray()) Primitive(item);
            }
            else Primitive(value);
        }
        var normalized = this with
        {
            Genres = Genres ?? [], Platforms = Platforms ?? [], Languages = Languages ?? [], Tags = Tags ?? [],
            SystemRequirements = SystemRequirements ?? [], Attributes = Attributes ?? [],
            Media = Media is null ? null : Media with { ScreenshotUrls = Media.ScreenshotUrls ?? [] }
        };
        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(normalized)) > 65536)
            throw new ArgumentException("Os detalhes excedem o limite de 64 KiB.");
        return normalized;
    }

    private static void Text(string? value, int max)
    {
        if (value?.Length > max) throw new ArgumentException($"Texto excede o limite de {max} caracteres.");
    }
    private static void List(string[]? values, int max)
    {
        if (values?.Length > max) throw new ArgumentException($"Lista excede o limite de {max} itens.");
        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Itens da lista não podem ser vazios.");
            Text(value, 100);
        }
    }
    private static void Key(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 80 || key.Contains('.') || key.Contains('$') || key.Contains('\0'))
            throw new ArgumentException("Chave inválida: use até 80 caracteres, sem ponto, $ ou caractere nulo.");
    }
    private static void Primitive(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String: Text(value.GetString(), 500); break;
            case JsonValueKind.Number when value.TryGetDouble(out var number) && double.IsFinite(number): break;
            case JsonValueKind.True: case JsonValueKind.False: case JsonValueKind.Null: break;
            default: throw new ArgumentException("Atributos aceitam valores simples ou arrays desses valores; objetos aninhados não são permitidos.");
        }
    }
    private static void Url(string? value, bool required = false)
    {
        if (value is null && !required) return;
        if (value?.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("URLs devem usar HTTP ou HTTPS e ter até 2048 caracteres.");
    }
    private static void Requirements(HardwareRequirements? value)
    {
        if (value is null) return;
        Text(value.Os, 200); Text(value.Processor, 200); Text(value.Graphics, 200);
        if (value.MemoryGb < 0 || value.StorageGb < 0) throw new ArgumentException("Memória e armazenamento não podem ser negativos.");
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GameMedia(string? CoverUrl = null, string[]? ScreenshotUrls = null, string? TrailerUrl = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlatformRequirements(HardwareRequirements? Minimum = null, HardwareRequirements? Recommended = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record HardwareRequirements(string? Os = null, string? Processor = null, int? MemoryGb = null, string? Graphics = null, int? StorageGb = null);
public sealed record GameDetailsResult(int SchemaVersion, GameDetailsContent Content, DateTime CreatedAt, DateTime UpdatedAt);
