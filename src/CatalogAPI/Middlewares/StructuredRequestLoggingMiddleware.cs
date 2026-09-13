using System.Diagnostics;

namespace CatalogAPI.Api.Middlewares
{
    public sealed class StructuredRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StructuredRequestLoggingMiddleware> _logger;

        public StructuredRequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<StructuredRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = Stopwatch.GetTimestamp();

            try
            {
                await _next(context);
            }
            finally
            {
                LogRequest(context, startedAt);
            }
        }

        private void LogRequest(HttpContext context, long startedAt)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                logLevel,
                "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMilliseconds} ms. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsedMilliseconds,
                context.TraceIdentifier);
        }
    }
}
