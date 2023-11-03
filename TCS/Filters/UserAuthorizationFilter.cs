using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Filters
{
    public class UserAuthorizationFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            string auth_token = context.HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(auth_token))
            {
                auth_token = context.HttpContext.Request.Cookies["auth_token"];
            }
            if (string.IsNullOrEmpty(auth_token) || !await Database.AuthArea.IsValidAuthToken(auth_token))
            {
                context.Result = new RedirectToPageResult("/Authorization");

                // Очищаем все куки пользователя
                foreach (var cookie in context.HttpContext.Request.Cookies.Keys)
                {
                    context.HttpContext.Response.Cookies.Delete(cookie);
                }
            }
        }
    }
}
