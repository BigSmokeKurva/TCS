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
                // TODO ������������� �� �������� ��������
                // ������������ �����������
                //Response.Redirect("/");
                Response.Redirect("/");
                return;
            }
            return;
        }
    }
}
