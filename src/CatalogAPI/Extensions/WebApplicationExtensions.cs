using CatalogAPI.Api.Middlewares;
using CatalogAPI.Api.Options;
using Prometheus;

namespace CatalogAPI.Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseApiPresentation(this WebApplication app)
        {
            app.UseRouting();
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
                pipeline => pipeline.UseHttpMetrics());
            UseRequestLogging(app);
            UseExceptionHandling(app);
            UseApiDocumentation(app);
            UseHttpPipeline(app);

            return app;
        }

        private static void UseRequestLogging(IApplicationBuilder app)
        {
            app.UseMiddleware<StructuredRequestLoggingMiddleware>();
        }

        private static void UseExceptionHandling(WebApplication app)
        {
            var apiOptions = app.Configuration
                .GetSection(ApiOptions.SectionName)
                .Get<ApiOptions>() ?? new ApiOptions();

            if (app.Environment.IsDevelopment() && apiOptions.UseDeveloperExceptionPage)
            {
                app.UseDeveloperExceptionPage();
                return;
            }

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }

        private static void UseApiDocumentation(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
                return;

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        private static void UseHttpPipeline(WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapMetrics("/metrics").AllowAnonymous();
        }
    }
}

