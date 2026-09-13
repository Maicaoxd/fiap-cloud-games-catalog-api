using CatalogAPI.Api.Common;
using CatalogAPI.Application.Common;
using CatalogAPI.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Api.Middlewares
{
    public sealed class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var problemDetails = CreateProblemDetails(context, exception);

                LogException(context, exception, problemDetails.Status!.Value);

                await WriteProblemDetailsAsync(context, problemDetails);
            }
        }

        private void LogException(HttpContext context, Exception exception, int statusCode)
        {
            if (exception is GameDetailsUnavailableException)
            {
                _logger.LogWarning("MongoDB unavailable while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
                return;
            }
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);

                return;
            }

            _logger.LogWarning(
                exception,
                "Handled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }

        private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problemDetails)
        {
            context.Response.StatusCode = problemDetails.Status!.Value;

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json");
        }

        private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
        {
            return exception switch
            {
                GameDetailsUnavailableException => CreateProblemDetails(
                    context, StatusCodes.Status503ServiceUnavailable,
                    "Serviço indisponível", exception.Message),
                BadHttpRequestException badRequest => CreateProblemDetails(
                    context,
                    badRequest.StatusCode,
                    ApiMessages.Validation.Title,
                    badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                        ? "O corpo da requisição excede o limite permitido."
                        : ApiMessages.Validation.RequestBodyRequired),

                ArgumentException => CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    ApiMessages.Validation.Title,
                    exception.Message),

                GameTitleAlreadyRegisteredException => CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    ApiMessages.Conflict.Title,
                    exception.Message),

                GameAlreadyOwnedException => CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    ApiMessages.Conflict.Title,
                    exception.Message),

                GameUnavailableException => CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    ApiMessages.Conflict.Title,
                    exception.Message),

                GameNotFoundException => CreateProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    ApiMessages.NotFound.Title,
                    exception.Message),

                InvalidCredentialsException => CreateProblemDetails(
                    context,
                    StatusCodes.Status401Unauthorized,
                    ApiMessages.Unauthorized.Title,
                    exception.Message),

                InvalidPaymentStatusException => CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    ApiMessages.Validation.Title,
                    exception.Message),

                DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException) => CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    ApiMessages.Conflict.Title,
                    ApplicationMessages.Conflict.UniqueConstraintViolation),

                _ => CreateProblemDetails(
                    context,
                    StatusCodes.Status500InternalServerError,
                    ApiMessages.InternalServerError.Title,
                    ApiMessages.InternalServerError.Detail)
            };
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            var message = exception.GetBaseException().Message;

            return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }

        private static ProblemDetails CreateProblemDetails(
            HttpContext context,
            int statusCode,
            string title,
            string detail)
        {
            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }
    }
}

