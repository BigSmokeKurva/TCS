using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Database;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppModel(DatabaseContext db) : PageModel
    {
        public readonly DatabaseContext db = db;
        public void OnGet()
        {
        }
    }
}
