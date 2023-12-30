using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TCS.Database;

namespace TCS.Filters
{
    public class AdminAuthorizationFilter(DatabaseContext db) : IAsyncAuthorizationFilter
    {
        private readonly DatabaseContext db = db;
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            string auth_token;
            if (context.HttpContext.Request.Path.StartsWithSegments("/api") && !context.HttpContext.Request.Path.StartsWithSegments("/api/auth/unauthorization"))
            {
                context.HttpContext.Request.Headers.Remove("Cookie");
                auth_token = context.HttpContext.Request.Headers["Authorization"];
            }
            else
            {
                auth_token = context.HttpContext.Request.Cookies["auth_token"];
            }
            if (!Guid.TryParse(auth_token, out Guid auth_token_uid) || !await db.Sessions.AnyAsync(x => x.AuthToken == auth_token_uid))
            {
                context.Result = new RedirectToPageResult("/Authorization");
                foreach (var cookie in context.HttpContext.Request.Cookies.Keys)
                {
                    context.HttpContext.Response.Cookies.Delete(cookie);
                }
                return;
            }
            if (await db.Users.Where(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == auth_token_uid)).Select(x => x.Paused).FirstAsync())
            {
                context.Result = new RedirectToPageResult("/Paused");
                return;
            }
            if (!await db.Users.Where(x => x.Id == db.Sessions.First(y => y.AuthToken == auth_token_uid).Id).Select(x => x.Admin).FirstAsync())
            {
                context.Result = new RedirectToPageResult("/App");
            }
        }
    }
}
