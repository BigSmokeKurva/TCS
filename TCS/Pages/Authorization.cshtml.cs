using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TCS.Pages
{
    public class AuthorizationModel : PageModel
    {
        public async Task OnGet()
        {
            var auth_token = Request.Cookies["auth_token"];
            if (auth_token is not null && await Database.AuthArea.IsValidAuthToken(auth_token))
                Response.Redirect("/App");
        }
    }
}
