namespace TCS.Middleware
{
    public class RemoveCookiesMiddleware
    {
        private readonly RequestDelegate _next;

        public RemoveCookiesMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/auth/unauthorization"))
            {
                context.Request.Headers.Remove("Cookie");
            }

            await _next(context);
        }
    }
}
