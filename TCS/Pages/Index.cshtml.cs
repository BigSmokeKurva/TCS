using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TCS.Pages
{
    public class IndexModel : PageModel
    {

        //public bool IsAuthorized;
        public async Task OnGet()
        {
            var auth_token = Request.Cookies.ContainsKey("auth_token") ? Request.Cookies["auth_token"] : null;
            if (auth_token is not null && await Database.IsValidAuthToken(auth_token))
            {
                // TODO переадресация на основную страницу
                // Пользователь авторизован
                Response.Redirect("/Error");
                return;
            }
            Response.Redirect("/Authorization");
            return;


        }
    }
}
