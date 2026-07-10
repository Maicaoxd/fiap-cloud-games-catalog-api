using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CatalogAPI.Api.Common;
using CatalogAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace CatalogAPI.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiPresentation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            AddControllers(services);
            AddJwtAuthentication(services, configuration);
            AddApiDocumentation(services);

            return services;
        }

        private static void AddControllers(IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory =
                        ApiValidationProblemDetailsFactory.CreateInvalidModelStateResponse;
                });
        }

        private static void AddApiDocumentation(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                ConfigureSwaggerAuthentication(options);
            });
        }

        private static void ConfigureSwaggerAuthentication(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
        {
            options.AddSecurityDefinition(
                JwtBearerDefaults.AuthenticationScheme,
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Informe apenas o token JWT, sem o prefixo Bearer.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        JwtBearerDefaults.AuthenticationScheme,
                        document,
                        externalResource: null)
                ] = []
            });
        }

        private static void AddJwtAuthentication(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtOptions = JwtOptions.Create(configuration);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateLifetime = true,
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = JwtClaimNames.Role,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            await WriteAuthenticationProblemDetailsAsync(
                                context.HttpContext,
                                StatusCodes.Status401Unauthorized);
                        },
                        OnForbidden = async context =>
                        {
                            await WriteAuthorizationProblemDetailsAsync(
                                context.HttpContext,
                                StatusCodes.Status403Forbidden);
                        }
                    };
                });

            services.AddAuthorization();
        }

        private static Task WriteAuthenticationProblemDetailsAsync(
            HttpContext httpContext,
            int statusCode)
        {
            return WriteProblemDetailsAsync(
                httpContext,
                statusCode,
                ApiMessages.Unauthorized.Title,
                ApiMessages.Unauthorized.Detail);
        }

        private static Task WriteAuthorizationProblemDetailsAsync(
            HttpContext httpContext,
            int statusCode)
        {
            return WriteProblemDetailsAsync(
                httpContext,
                statusCode,
                ApiMessages.Forbidden.Title,
                ApiMessages.Forbidden.Detail);
        }

        private static async Task WriteProblemDetailsAsync(
            HttpContext httpContext,
            int statusCode,
            string title,
            string detail)
        {
            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json");
        }
    }
}

