using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Database;

namespace TCS.Pages
{
    public class PausedModel(DatabaseContext db) : PageModel
    {
        public readonly DatabaseContext db = db;
        public void OnGet()
        {
            var auth_token = Guid.Parse(Request.Cookies["auth_token"]);
            var Paused = db.Users.First(x => x.Sessions.Any(y => y.AuthToken == auth_token && y.Id == x.Id)).Paused;
            if (!Paused)
            {
                Response.Redirect("/App");
                return;
            }
        }
    }
}
