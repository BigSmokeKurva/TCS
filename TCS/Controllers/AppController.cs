using Microsoft.AspNetCore.Mvc;
using TCS.Filters;

namespace TCS.Controllers
{
    [Route("App")]
    [TypeFilter(typeof(AuthTokenPageFilter))]
    public class AppController : Controller
    {
        [HttpGet("LoadPartialView")]
        public async Task<IActionResult> LoadPartialView(string partialViewName)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            if (auth_token is not null && await Database.IsValidAuthToken(auth_token))
            {
                return PartialView(partialViewName);
            }
            foreach (var cookie in Response.HttpContext.Request.Cookies.Keys)
            {
                Response.HttpContext.Response.Cookies.Delete(cookie);
            }
            return Unauthorized();
        }
    }
}
