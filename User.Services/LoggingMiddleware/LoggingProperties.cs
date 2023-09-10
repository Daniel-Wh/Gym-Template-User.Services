using System.Diagnostics;
using System.Web;
using Serilog.Context;

namespace User.Services
{
    public class LoggingPropertiesMiddleware
    {
        private readonly ILogger<LoggingPropertiesMiddleware> _logger;
        private readonly RequestDelegate _next;


        public LoggingPropertiesMiddleware(RequestDelegate next, ILogger<LoggingPropertiesMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // HeaderDictionary uses a case-insensitive backing dictionary; normalization not necessary with HeaderDictionary calls
            // Additionally, it returns empty value if property is not found
            // https://github.com/dotnet/aspnetcore/blob/c85baf8db0c72ae8e68643029d514b2e737c9fae/src/Http/Http/src/HeaderDictionary.cs#L54

            var correlationId = context.Request.Headers[CorrelationIdHeader.HeaderKey];

            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                // _logger.LogTrace($"Generated correlation id {correlationId}");
            }

            // _logger.LogTrace($"read correlation id {correlationId}");

            context.Response.Headers.Add(CorrelationIdHeader.HeaderKey, HttpUtility.UrlEncode(correlationId));

            using (LogContext.PushProperty(CorrelationIdHeader.HeaderKey, correlationId))
            using (LogContext.PushProperty("http-status", context.Response.StatusCode.ToString()))
            using (LogContext.PushProperty("requested-url", GetRawUrl(context.Request)))
            {
                var watch = Stopwatch.StartNew();
                await _next(context);
                watch.Stop();
                _logger.LogInformation($"Request finished in {watch.ElapsedMilliseconds}ms");
            }
        }

        public static string GetRawUrl(HttpRequest request)
        {
            var httpContext = request.HttpContext;
            return
                $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        }

        public static class CorrelationIdHeader
        {
            public static string HeaderKey = "gtl-correlation-id";
        }
    }
}