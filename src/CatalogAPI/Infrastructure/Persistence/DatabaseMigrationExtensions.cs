using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence
{
    public static class DatabaseMigrationExtensions
    {
        public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseMigration");

            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    logger.LogInformation("Aplicando migrations do banco da CatalogAPI. Tentativa {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Migrations do banco da CatalogAPI aplicadas com sucesso.");
                    return;
                }
                catch (Exception exception) when (attempt < maxAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Falha ao aplicar migrations do banco da CatalogAPI. Nova tentativa em 5 segundos. Tentativa {Attempt}/{MaxAttempts}.",
                        attempt,
                        maxAttempts);

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            await dbContext.Database.MigrateAsync();
        }
    }
}
