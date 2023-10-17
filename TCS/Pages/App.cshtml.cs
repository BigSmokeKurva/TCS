using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(AuthTokenPageFilter))]
    public class AppModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
