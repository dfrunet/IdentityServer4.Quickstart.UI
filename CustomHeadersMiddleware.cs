using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdSrvHost
{
    public static class CustomHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomHeaders(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomHeadersMiddleware>();
        }
    }
    public class CustomHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomHeadersMiddleware> _logger;

        public CustomHeadersMiddleware(RequestDelegate next, ILogger<CustomHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var watch = new Stopwatch();
            watch.Start();

            //To add Headers AFTER everything you need to do this
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Response-Time-Milliseconds"))
                    context.Response.Headers.Add("X-Response-Time-Milliseconds",
                        new[] { watch.ElapsedMilliseconds.ToString() });

                return Task.FromResult(0);
            });

            await _next(context);
        }
    }
}
