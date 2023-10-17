using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Filters
{
    public class AuthTokenPageFilter : IAsyncPageFilter
    {
        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var auth_token = context.HttpContext.Request.Cookies["auth_token"];
            if (auth_token is not null && await Database.IsValidAuthToken(auth_token))
            {
                await next.Invoke();
                return;
            }
            context.Result = new RedirectToPageResult("/Authorization");
            foreach (var cookie in context.HttpContext.Request.Cookies.Keys)
            {
                context.HttpContext.Response.Cookies.Delete(cookie);
            }
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
