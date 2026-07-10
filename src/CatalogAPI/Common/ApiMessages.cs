namespace CatalogAPI.Api.Common
{
    public static class ApiMessages
    {
        public static class Validation
        {
            public const string Title = "Erro de validacao.";
            public const string InvalidFields = "Um ou mais campos sao invalidos.";
            public const string RequestBodyRequired = "O corpo da requisicao e obrigatorio.";
        }

        public static class Game
        {
            public const string TitleRequired = "Titulo do jogo e obrigatorio.";
            public const string DescriptionRequired = "Descricao do jogo e obrigatoria.";
            public const string PriceCannotBeNegative = "Preco do jogo nao pode ser negativo.";
        }

        public static class Conflict
        {
            public const string Title = "Conflito.";
        }

        public static class NotFound
        {
            public const string Title = "Recurso nao encontrado.";
        }

        public static class Unauthorized
        {
            public const string Title = "Nao autorizado.";
            public const string Detail = "A autenticacao e necessaria para acessar este recurso.";
        }

        public static class Forbidden
        {
            public const string Title = "Acesso negado.";
            public const string Detail = "Voce nao tem permissao para acessar este recurso.";
        }

        public static class InternalServerError
        {
            public const string Title = "Erro interno.";
            public const string Detail = "Ocorreu um erro inesperado.";
        }
    }
}

