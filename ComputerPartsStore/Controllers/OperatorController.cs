using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ComputerPartsStore.Data;
using ComputerPartsStore.Models;

namespace ComputerPartsStore.Controllers
{
    [Authorize(Policy = "Operator")]
    public class OperatorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OperatorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Order_lists
                .Include(o => o.Order_Status)
                .Include(o => o.Customer)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .OrderBy(o => o.Order_Date)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Order_lists
                .Include(o => o.Order_Status)
                .Include(o => o.Customer)
                .ThenInclude(c => c.Address)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .ThenInclude(a => a.Catalog)
                .Include(o => o.Logs)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(o => o.Order_id == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.OrderStatuses = await _context.Order_Statuses.ToListAsync();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int statusId, string notes = "")
        {
            var order = await _context.Order_lists.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Замовлення не знайдено" });
            }

            var oldStatusId = order.Order_status_id;
            order.Order_status_id = statusId;

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Add tracking number if status is "Доставляється"
            if (statusId == 5 && string.IsNullOrEmpty(order.TrackingNumber))
            {
                order.TrackingNumber = GenerateTrackingNumber();
            }

            // Add log entry
            var log = new Log
            {
                Order_id = orderId,
                User_id = userId,
                Last_Change = DateTime.Now,
                Action = $"Статус змінено з {GetStatusName(oldStatusId)} на {GetStatusName(statusId)}"
            };

            if (!string.IsNullOrEmpty(notes))
            {
                log.Action += $". Примітка: {notes}";
            }

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Статус успішно оновлено",
                trackingNumber = order.TrackingNumber
            });
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Accessories
                .Include(a => a.Catalog)
                .OrderBy(a => a.Catalog_id)
                .ThenBy(a => a.Accessory_Name)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _context.Catalogs.ToListAsync();
            return View(new Accessories());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Accessories product)
        {
            if (ModelState.IsValid)
            {
                _context.Accessories.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успішно додано";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = await _context.Catalogs.ToListAsync();
            return View(product);
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Accessories.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Catalogs.ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Accessories product)
        {
            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успішно оновлено";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = await _context.Catalogs.ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Accessories.FindAsync(id);
            if (product != null)
            {
                // Check if product is used in any orders
                var hasOrders = await _context.Order_Items
                    .AnyAsync(oi => oi.Accessory_id == id);

                if (hasOrders)
                {
                    return Json(new { success = false, message = "Неможливо видалити товар, який використовується в замовленнях" });
                }

                _context.Accessories.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Товар успішно видалено" });
            }

            return Json(new { success = false, message = "Товар не знайдено" });
        }

        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                TotalOrders = await _context.Order_lists.CountAsync(),
                NewOrders = await _context.Order_lists.CountAsync(o => o.Order_status_id == 1),
                CompletedOrders = await _context.Order_lists.CountAsync(o => o.Order_status_id == 6),
                TotalProducts = await _context.Accessories.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role_Name == "Customer"),
                RecentOrders = await _context.Order_lists
                    .Include(o => o.Customer)
                    .Include(o => o.Order_Status)
                    .OrderByDescending(o => o.Order_Date)
                    .Take(10)
                    .ToListAsync()
            };

            return View(stats);
        }

        private string GenerateTrackingNumber()
        {
            return "TRK" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);
        }

        private string GetStatusName(int statusId)
        {
            return statusId switch
            {
                1 => "Нове",
                2 => "Прийняте",
                3 => "Формується",
                4 => "Сформоване",
                5 => "Доставляється",
                6 => "Доставлене",
                7 => "Скасоване",
                _ => "Невідомо"
            };
        }
    }
}