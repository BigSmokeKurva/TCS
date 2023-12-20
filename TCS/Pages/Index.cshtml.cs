using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TCS.Database;

namespace TCS.Pages
{
    public class IndexModel(DatabaseContext db) : PageModel
    {
        public readonly DatabaseContext db = db;
        public async Task OnGet()
        {
            if (Guid.TryParse(Request.Cookies["auth_token"], out var auth_token) && await db.Sessions.AnyAsync(x => x.AuthToken == auth_token))
            {
                Response.Redirect("/App");
                return;
            }
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            Response.Redirect("/Authorization");
        }
    }
}
