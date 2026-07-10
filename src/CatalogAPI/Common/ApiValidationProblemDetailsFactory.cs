using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Api.Common
{
    public static class ApiValidationProblemDetailsFactory
    {
        public static IActionResult CreateInvalidModelStateResponse(ActionContext context)
        {
            var errors = context.ModelState
                .Where(modelState => modelState.Value?.Errors.Count > 0)
                .ToDictionary(
                    modelState => ToCamelCase(modelState.Key),
                    modelState => modelState.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ApiMessages.Validation.Title,
                Detail = ApiMessages.Validation.InvalidFields,
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails);
        }

        private static string ToCamelCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return char.ToLowerInvariant(value[0]) + value[1..];
        }
    }
}

