using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TCS.Pages
{
    public class IndexModel : PageModel
    {

        //public bool IsAuthorized;
        public ActionResult OnGet()
        {
            var auth_token = Request.Cookies.ContainsKey("auth_token") ? Request.Cookies["auth_token"] : null;
            if(auth_token is not null)
            {
                // Проверка токена авторизации
                return Redirect("/Index");
            }
            return Redirect("/Authorization");
        }
    }
}
