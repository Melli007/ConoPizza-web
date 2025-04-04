using ConoPizzaWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConoPizzaWeb.Controllers
{
    public class MenuController : Controller
    {

        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Pizza()
        {
            // Query only products whose Category name is "Pizza".
            var pizzas = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Prices)
                .Where(p => p.Category.Name.ToLower() == "pizza")
                .ToListAsync();

            return View(pizzas);
        }

        public async  Task<IActionResult> burgers()
        {
            var burgers = await _context.Products
                .Include(b => b.Category)
                .Include(b => b.Prices)
                .Where(b => b.Category.Name.ToLower() == "burgers")
                .ToListAsync();

            return View(burgers);
        }

        public async Task<IActionResult> sandwiches()
        {
            var sandwiches = await _context.Products
                .Include(s => s.Category)
                .Include(s => s.Prices)
                .Where(s => s.Category.Name.ToLower() == "sandwiches")
                .ToListAsync();

            return View(sandwiches);
        }
    }
}
