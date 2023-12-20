using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCS.Database;

namespace TCS.Controllers
{
    [Route("App")]
    public class AppController(DatabaseContext db) : Controller
    {
        public readonly DatabaseContext db = db;

        [HttpGet("LoadPartialView")]
        public async Task<IActionResult> LoadPartialView(string partialViewName)
        {
            if (Guid.TryParse(Request.Headers.Authorization, out var auth_token) && await db.Sessions.AnyAsync(x => x.AuthToken == auth_token))
            {
                return PartialView(partialViewName, this);
            }
            foreach (var cookie in Response.HttpContext.Request.Cookies.Keys)
            {
                Response.HttpContext.Response.Cookies.Delete(cookie);
            }
            return Unauthorized();
        }
    }
}
