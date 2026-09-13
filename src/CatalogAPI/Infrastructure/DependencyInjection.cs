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
using CatalogAPI.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;
using CatalogAPI.Application.Abstractions.Caching;
using CatalogAPI.Infrastructure.Caching;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace CatalogAPI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            AddPersistence(services, configuration);
            AddMongoPersistence(services, configuration);
            AddRedisCache(services, configuration);
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

        private static void AddMongoPersistence(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<MongoDbOptions>()
                .Bind(configuration.GetSection(MongoDbOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "MongoDb:ConnectionString is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.DatabaseName), "MongoDb:DatabaseName is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.CollectionName), "MongoDb:CollectionName is required.")
                .Validate(o => string.IsNullOrEmpty(o.Username) == string.IsNullOrEmpty(o.Password), "MongoDb:Username and Password must be supplied together.")
                .Validate(o => o.OperationTimeoutSeconds is >= 1 and <= 10, "MongoDb:OperationTimeoutSeconds must be between 1 and 10.")
                .ValidateOnStart();
            services.AddSingleton<IMongoClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
                var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
                if (!string.IsNullOrEmpty(options.Username))
                    settings.Credential = MongoCredential.CreateCredential(options.DatabaseName, options.Username, options.Password);
                var timeout = TimeSpan.FromSeconds(options.OperationTimeoutSeconds);
                settings.ServerSelectionTimeout = timeout;
                settings.ConnectTimeout = timeout;
                settings.SocketTimeout = timeout;
                return new MongoClient(settings);
            });
            services.AddScoped<IGameDetailsRepository, MongoGameDetailsRepository>();
        }

        private static void AddRedisCache(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<Caching.RedisOptions>()
                .Bind(configuration.GetSection(Caching.RedisOptions.SectionName))
                .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Configuration), "Redis:Configuration is required when caching is enabled.")
                .ValidateOnStart();
            services.AddStackExchangeRedisCache(_ => { });
            services.AddOptions<RedisCacheOptions>().Configure<IOptions<Caching.RedisOptions>>((cache, configured) =>
            {
                var options = configured.Value;
                var connection = ConfigurationOptions.Parse(options.Enabled ? options.Configuration : "localhost:6379");
                if (!string.IsNullOrEmpty(options.Password)) connection.Password = options.Password;
                connection.AbortOnConnectFail = false;
                connection.ConnectTimeout = 1000;
                connection.AsyncTimeout = 1000;
                connection.SyncTimeout = 1000;
                connection.ConnectRetry = 0;
                connection.BacklogPolicy = BacklogPolicy.FailFast;
                cache.ConfigurationOptions = connection;
                cache.InstanceName = "fcg:catalog:";
            });
            services.AddScoped<IGameCache, RedisGameCache>();
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
                x.AddConsumer<PaymentProcessedEventConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "catalog-payment-processed-event");

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

