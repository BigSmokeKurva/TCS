using Microsoft.AspNetCore.Mvc;

namespace TCS.Controllers
{
    [Route("App")]
    public class AppController : Controller
    {
        [HttpGet("LoadPartialView")]
        public async Task<IActionResult> LoadPartialView(string partialViewName)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            if (auth_token is not null && await Database.AuthArea.IsValidAuthToken(auth_token))
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
