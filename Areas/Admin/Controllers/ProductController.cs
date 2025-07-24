using Bangaliyana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            return View(_db.Products.Include(p=>p.ProductTypes).Include(s=>s.SpecialTags).ToList());
        }
    }
}
