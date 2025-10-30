using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChatApp.Shared.Infrastructure.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopWatch=Stopwatch.StartNew();
            var requestPath= context.Request.Path;
            var requestMethod= context.Request.Method;

            _logger?.LogInformation("Incoming request: {Method} {Path}",requestMethod, requestPath);

            try
            {
                await _next(context);
            }
            finally
            {
                stopWatch.Stop();
                var statusCode=context.Response.StatusCode;
                var elapsedMs=stopWatch.ElapsedMilliseconds;

                _logger?.LogInformation(
                    "Completed request: {Method} {Path} - Status: {StatusCode} - Duration: {ElapsedMs}",
                    requestMethod,
                    requestPath,
                    statusCode,
                    elapsedMs);
            }
        }
    }
}