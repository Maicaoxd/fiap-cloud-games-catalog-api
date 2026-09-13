namespace CatalogAPI.Application.Common.Exceptions;

public sealed class GameDetailsUnavailableException : Exception
{
    public GameDetailsUnavailableException(Exception? innerException = null)
        : base("O armazenamento dos detalhes do jogo está temporariamente indisponível.", innerException) { }
}
