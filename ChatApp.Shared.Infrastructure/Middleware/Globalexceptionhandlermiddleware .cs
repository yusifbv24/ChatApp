using ChatApp.Shared.Kernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ChatApp.Shared.Infrastructure.Middleware
{
    public class Globalexceptionhandlermiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Globalexceptionhandlermiddleware> _logger;

        public Globalexceptionhandlermiddleware(
            RequestDelegate next,
            ILogger<Globalexceptionhandlermiddleware> logger)
        {
            _next= next;
            _logger= logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, error, errors) = exception switch
            {
                NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message, (IDictionary<string, string[]>?)null),
                ValidationException validationEx => (HttpStatusCode.BadRequest, "Validation failed", validationEx.Errors),
                DomainException domainEx => (HttpStatusCode.BadRequest, domainEx.Message, null),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred. Please try again later.", null)
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                error,
                errors,
                timestamp = DateTime.UtcNow
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}