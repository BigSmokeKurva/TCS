using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Filters
{
    public class AdminAuthorizationFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            string auth_token = context.HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(auth_token))
            {
                auth_token = context.HttpContext.Request.Cookies["auth_token"];
            }
            if (string.IsNullOrEmpty(auth_token) ||
                !(await Database.AuthArea.IsValidAuthToken(auth_token) && await Database.SharedArea.IsAdmin(await Database.SharedArea.GetId(auth_token))))
            {
                context.Result = new RedirectToPageResult("/App");
            }
        }
    }
}
