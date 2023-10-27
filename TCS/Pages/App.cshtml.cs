using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
