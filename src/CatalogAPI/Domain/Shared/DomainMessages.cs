namespace CatalogAPI.Domain.Shared
{
    public static class DomainMessages
    {
        public static class Entity
        {
            public const string ResponsibleForChangeRequired = "Responsavel pela alteracao e obrigatorio.";
        }

        public static class Game
        {
            public const string TitleRequired = "Titulo do jogo e obrigatorio.";
            public const string DescriptionRequired = "Descricao do jogo e obrigatoria.";
            public const string PriceCannotBeNegative = "Preco do jogo nao pode ser negativo.";
        }

        public static class Library
        {
            public const string UserIdRequired = "Usuario da biblioteca e obrigatorio.";
            public const string GameIdRequired = "Jogo da biblioteca e obrigatorio.";
        }

        public static class Order
        {
            public const string UserIdRequired = "Usuario do pedido e obrigatorio.";
            public const string ItemsRequired = "Pedido deve possuir ao menos um jogo.";
            public const string DuplicateItemsNotAllowed = "Pedido nao pode possuir jogos duplicados.";
            public const string FinalPaymentStatusRequired = "Status final de pagamento e obrigatorio.";
        }

        public static class OrderItem
        {
            public const string GameIdRequired = "Jogo do item do pedido e obrigatorio.";
            public const string PriceCannotBeNegative = "Preco do item do pedido nao pode ser negativo.";
        }
    }
}
