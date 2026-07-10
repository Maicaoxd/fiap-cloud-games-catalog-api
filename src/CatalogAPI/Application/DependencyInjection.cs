using CatalogAPI.Application.Games.Create;
using CatalogAPI.Application.Games.Deactivate;
using CatalogAPI.Application.Games.Get;
using CatalogAPI.Application.Games.List;
using CatalogAPI.Application.Games.Update;
using CatalogAPI.Application.Libraries.List;
using CatalogAPI.Application.Payments.ProcessPaymentResult;
using CatalogAPI.Application.Purchases.PurchaseGame;

namespace CatalogAPI.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            AddGameUseCases(services);
            AddLibraryUseCases(services);
            AddPurchaseUseCases(services);
            AddPaymentUseCases(services);

            return services;
        }

        private static void AddGameUseCases(IServiceCollection services)
        {
            services.AddScoped<CreateGameUseCase>();
            services.AddScoped<DeactivateGameUseCase>();
            services.AddScoped<GetGameUseCase>();
            services.AddScoped<ListGamesUseCase>();
            services.AddScoped<UpdateGameUseCase>();
        }

        private static void AddLibraryUseCases(IServiceCollection services)
        {
            services.AddScoped<ListLibraryGamesUseCase>();
        }

        private static void AddPurchaseUseCases(IServiceCollection services)
        {
            services.AddScoped<PurchaseGameUseCase>();
        }

        private static void AddPaymentUseCases(IServiceCollection services)
        {
            services.AddScoped<ProcessPaymentResultUseCase>();
        }
    }
}

