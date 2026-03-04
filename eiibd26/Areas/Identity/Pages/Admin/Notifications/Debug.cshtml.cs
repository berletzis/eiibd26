using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Notifications
{
    [Authorize(Roles = "Administrador")]
    public class DebugModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DebugModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<eiibd26.Models.NotificationSubscription> Subscriptions { get; set; } = new();
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int InactiveSubscriptions { get; set; }

        public async Task OnGetAsync()
        {
            Subscriptions = await _db.NotificationSubscriptions
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            TotalSubscriptions = Subscriptions.Count;
            ActiveSubscriptions = Subscriptions.Count(s => s.IsActive);
            InactiveSubscriptions = Subscriptions.Count(s => !s.IsActive);
        }
    }
}
