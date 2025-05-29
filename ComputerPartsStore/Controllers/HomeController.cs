using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerPartsStore.Data;
using ComputerPartsStore.Models;
using System.Diagnostics;

namespace ComputerPartsStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _context.Accessories
                .Include(a => a.Catalog)
                .Take(6)
                .ToListAsync();

            ViewBag.Categories = await _context.Catalogs.ToListAsync();

            return View(featuredProducts);
        }

        public async Task<IActionResult> Catalog(int? categoryId, string search)
        {
            var query = _context.Accessories.Include(a => a.Catalog).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(a => a.Catalog_id == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Accessory_Name.Contains(search) ||
                                       a.Specifications.Contains(search));
            }

            var products = await query.ToListAsync();

            ViewBag.Categories = await _context.Catalogs.ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.Search = search;

            return View(products);
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Accessories
                .Include(a => a.Catalog)
                .FirstOrDefaultAsync(a => a.Accessory_id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}