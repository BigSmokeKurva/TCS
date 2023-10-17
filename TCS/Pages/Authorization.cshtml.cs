using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TCS.Pages
{
    public class AuthorizationModel : PageModel
    {
        public async Task OnGet()
        {
            var auth_token = Request.Cookies["auth_token"];
            if (auth_token is not null && await Database.IsValidAuthToken(auth_token))
            {
                // TODO переадресация на основную страницу
                // Пользователь авторизован
                //Response.Redirect("/");
                Response.Redirect("/");
                return;
            }
            return;
        }
    }
}
