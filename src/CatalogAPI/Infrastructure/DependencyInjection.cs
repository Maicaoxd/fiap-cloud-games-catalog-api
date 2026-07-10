using CatalogAPI.Application.Abstractions.Messaging;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Health;
using CatalogAPI.Infrastructure.Messaging;
using CatalogAPI.Infrastructure.Messaging.Consumers;
using CatalogAPI.Infrastructure.Persistence;
using CatalogAPI.Infrastructure.Persistence.Repositories;
using CatalogAPI.Infrastructure.Security;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CatalogAPI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            AddPersistence(services, configuration);
            AddSecurity(services, configuration);
            AddMessaging(services, configuration);

            return services;
        }

        private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection connection string was not configured.");

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker>();
            services.AddScoped<IGameRepository, GameRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
        }

        private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(JwtOptions.Create(configuration));
        }

        private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<RabbitMqOptions>()
                .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq:Host is required.")
                .Validate(options => options.Port is > 0 and <= 65535, "RabbitMq:Port must be between 1 and 65535.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.VirtualHost), "RabbitMq:VirtualHost is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "RabbitMq:Username is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMq:Password is required.")
                .ValidateOnStart();

            services.AddSingleton<IRabbitMqConnectionChecker, RabbitMqConnectionChecker>();
            services.AddScoped<IOrderPlacedEventPublisher, MassTransitOrderPlacedEventPublisher>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<PaymentProcessedEventConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    var virtualHostPath = rabbitMqOptions.VirtualHost == "/"
                        ? string.Empty
                        : Uri.EscapeDataString(rabbitMqOptions.VirtualHost.TrimStart('/'));

                    var hostAddress = new UriBuilder("rabbitmq", rabbitMqOptions.Host, rabbitMqOptions.Port, virtualHostPath).Uri;

                    cfg.Host(hostAddress, h =>
                    {
                        h.Username(rabbitMqOptions.Username);
                        h.Password(rabbitMqOptions.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
        }
    }
}

