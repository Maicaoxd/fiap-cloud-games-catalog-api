using CatalogAPI.Domain.Games;

namespace CatalogAPI.Application.Games.Search;

public sealed record GameSearchDocument(Guid GameId, string Title, string Description, decimal Price, bool IsActive, DateTime UpdatedAt)
{
    public static GameSearchDocument From(Game game) =>
        new(game.Id, game.Title, game.Description, game.Price, game.IsActive, game.UpdatedAt ?? game.CreatedAt);
}
public sealed record GameSearchHit(Guid GameId, string Title, string Description, decimal Price, double Score);
public sealed record GameSearchResult(long Total, int Page, int PageSize, IReadOnlyList<GameSearchHit> Items);

public interface IGameSearchIndex
{
    Task<GameSearchResult> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken);
    Task IndexAsync(GameSearchDocument game, long version, CancellationToken cancellationToken);
}
public sealed class GameSearchUnavailableException : Exception
{
    public GameSearchUnavailableException() : base("A busca de jogos está temporariamente indisponível.") { }
}

public sealed class SearchGamesUseCase(IGameSearchIndex index)
{
    public Task<GameSearchResult> ExecuteAsync(string? query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 150)
            throw new ArgumentException("Informe uma busca entre 1 e 150 caracteres.");
        if (page < 1 || pageSize is < 1 or > 50 || ((long)page - 1) * pageSize + pageSize > 10000)
            throw new ArgumentException("Paginação inválida: use até 50 itens por página e uma janela de até 10000 resultados.");
        return index.SearchAsync(text, page, pageSize, cancellationToken);
    }
}
