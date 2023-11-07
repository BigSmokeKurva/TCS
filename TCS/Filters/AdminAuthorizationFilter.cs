using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Filters
{
    public class AdminAuthorizationFilter : IAsyncAuthorizationFilter
    {
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
            if (string.IsNullOrEmpty(auth_token) || !await Database.AuthArea.IsValidAuthToken(auth_token))
            {
                context.Result = new RedirectToPageResult("/Authorization");
                return;
            }
            if (!await Database.SharedArea.IsAdmin(await Database.SharedArea.GetId(auth_token)))
            {
                context.Result = new RedirectToPageResult("/App");
            }
        }
    }
}
