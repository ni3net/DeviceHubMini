namespace DeviceHubMini.Client.Security
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string? _expectedKey;
        private readonly IWebHostEnvironment _env;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration cfg, IWebHostEnvironment env)
        {
            _next = next;
            _expectedKey = cfg["GraphQL:ApiKey"];
            _env = env;
        }

        public async Task Invoke(HttpContext ctx)
        {
            // Allow health checks and static assets
            if (ctx.Request.Path.StartsWithSegments("/healthz") ||
                ctx.Request.Path.StartsWithSegments("/favicon.ico"))
            {
                await _next(ctx);
                return;
            }

            // 🚨 Skip auth in Development for Banana Cake Pop IDE
            if (_env.IsDevelopment())
            {
                await _next(ctx);
                return;
            }

            // Enforce API key for actual GraphQL POSTs
            if (ctx.Request.Path.StartsWithSegments("/graphql"))
            {
                if (string.IsNullOrWhiteSpace(_expectedKey))
                {
                    await _next(ctx);
                    return;
                }

                if (!ctx.Request.Headers.TryGetValue("x-api-key", out var provided) ||
                    !string.Equals(provided, _expectedKey, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            await _next(ctx);
        }
    }
}
