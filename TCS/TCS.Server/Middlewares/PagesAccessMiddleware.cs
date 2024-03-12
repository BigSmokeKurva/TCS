using Microsoft.EntityFrameworkCore;
using TCS.Server.Database;

namespace TCS.Server.Middlewares
{
    public class PagesAccessMiddleware
    {
        private readonly DatabaseContext _db;
        private readonly RequestDelegate _next;

        public PagesAccessMiddleware(DatabaseContext db, RequestDelegate next)
        {
            _db = db;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
                var path = context.Request.Path;
            if (context.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/app");
                return;
            }

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                string authorizationHeader = context.Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    context.Response.Redirect("/signin");
                    return;
                }
                if (!Guid.TryParse(authorizationHeader, out Guid authToken) || !await _db.Sessions.AnyAsync(x => x.AuthToken == authToken))
                {
                    context.Response.Redirect("/signin");
                    foreach (var cookie in context.Request.Cookies.Keys)
                    {
                        context.Response.Cookies.Delete(cookie);
                    }
                    return;
                }
                var id = await _db.Sessions.Where(x => x.AuthToken == authToken).Select(x => x.Id).FirstAsync();
                var info = await _db.Users.Where(x => x.Id == id).Select(x => new
                {
                    isPaused = x.Paused,
                    isAdmin = x.Admin
                }).FirstOrDefaultAsync();
                if (info is null)
                {
                    context.Response.Redirect("/signin");
                    return;
                }
                if (info.isPaused)
                {
                    context.Response.Redirect("/paused");
                    return;
                }
                if (path.StartsWithSegments("/api/app"))
                {
                    await _next(context);
                    return;
                }
                if (path.StartsWithSegments("/api/admin") && !info.isAdmin)
                {
                    context.Response.Redirect("/app");
                    return;
                }
                await _next(context);
            }
            else
            {
                if (!context.Request.Cookies.TryGetValue("auth_token", out string _authToken) &&
                    path.StartsWithSegments("signin") || path.StartsWithSegments("signup"))
                {
                    context.Response.Redirect("/signin");
                    return;
                }
                if (!Guid.TryParse(_authToken, out Guid authToken) || !await _db.Sessions.AnyAsync(x => x.AuthToken == authToken))
                {
                    context.Response.Redirect("/signin");
                    foreach (var cookie in context.Request.Cookies.Keys)
                    {
                        context.Response.Cookies.Delete(cookie);
                    }
                    return;
                }
                var id = await _db.Sessions.Where(x => x.AuthToken == authToken).Select(x => x.Id).FirstAsync();
                var info = await _db.Users.Where(x => x.Id == id).Select(x => new
                {
                    isPaused = x.Paused,
                    isAdmin = x.Admin
                }).FirstAsync();

                if (info.isPaused)
                {
                    context.Response.Redirect("/paused");
                    return;
                }


                if (path.StartsWithSegments("/app"))
                {
                    await _next(context);
                    return;
                }
                if ((path.StartsWithSegments("/admin-panel") || path.StartsWithSegments("/invite-codes")) && !info.isAdmin)
                {
                    context.Response.Redirect("/app");
                    return;
                }
                if (path.StartsWithSegments("/signin") || path.StartsWithSegments("/signup"))
                {
                    context.Response.Redirect("/app");
                    return;
                }
                await _next(context);
            }
        }
    }
}
