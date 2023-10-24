using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(AuthTokenPageFilter))]
    public class AdminPanelModel : PageModel
    {
        public async Task OnGet()
        {
            var auth_token = Request.Cookies["auth_token"];
            if (!(auth_token is not null && await Database.IsAdmin(await Database.GetId(auth_token))))
            {
                Response.Redirect("/");
            }
        }
    }
}
