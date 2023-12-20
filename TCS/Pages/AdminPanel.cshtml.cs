using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TCS.Database;
using TCS.Filters;

namespace TCS.Pages
{
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public class AdminPanelModel(DatabaseContext db) : PageModel
    {
        public readonly DatabaseContext db = db;
        public async Task OnGet()
        {
        }
    }
}
