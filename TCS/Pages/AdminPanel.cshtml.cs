using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public class AdminPanelModel : PageModel
    {
        public async Task OnGet()
        {
        }
    }
}
