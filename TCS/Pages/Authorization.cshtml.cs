using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TCS.Pages
{
    public class AuthorizationModel : PageModel
    {
        public async Task OnGet()
        {
            var auth_token = Request.Cookies.ContainsKey("auth_token") ? Request.Cookies["auth_token"] : null;
            if (auth_token is not null && await Database.IsValidAuthToken(auth_token))
            {
                // TODO ������������� �� �������� ��������
                // ������������ �����������
                //Response.Redirect("/");
                Response.Redirect("/Error");
                return;
            }
            return;
        }
    }
}
