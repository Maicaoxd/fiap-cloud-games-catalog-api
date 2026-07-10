namespace CatalogAPI.Application.Common
{
    public static class ApplicationMessages
    {
        public static class Authentication
        {
            public const string InvalidCredentials = "Token JWT invalido ou ausente.";
        }

        public static class Game
        {
            public const string TitleAlreadyRegistered = "Jogo ja cadastrado com este titulo.";
            public const string NotFound = "Um ou mais jogos nao foram encontrados.";
            public const string InactiveCannotBePurchased = "Um ou mais jogos inativos nao podem ser comprados.";
        }

        public static class Library
        {
            public const string GameAlreadyOwned = "Um ou mais jogos ja estao na biblioteca do usuario.";
        }

        public static class Payment
        {
            public const string InvalidStatus = "Status de pagamento invalido.";
        }

        public static class Conflict
        {
            public const string UniqueConstraintViolation = "Ja existe um registro com os mesmos dados.";
        }
    }
}
