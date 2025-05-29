using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ComputerPartsStore.Data;
using ComputerPartsStore.Models;

namespace ComputerPartsStore.Controllers
{
    [Authorize(Policy = "Storekeeper")]
    public class StorekeeperController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StorekeeperController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Show orders that are ready for processing (status 2-4)
            var orders = await _context.Order_lists
                .Include(o => o.Order_Status)
                .Include(o => o.Customer)
                .ThenInclude(c => c.Address)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .Where(o => o.Order_status_id >= 2 && o.Order_status_id <= 5)
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

            // Only show relevant statuses for storekeeper
            var availableStatuses = await _context.Order_Statuses
                .Where(s => s.Order_status_id >= 3 && s.Order_status_id <= 7)
                .ToListAsync();

            ViewBag.OrderStatuses = availableStatuses;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPacking(int orderId)
        {
            return await UpdateOrderStatus(orderId, 3, "Комірник розпочав збирання замовлення");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishPacking(int orderId)
        {
            return await UpdateOrderStatus(orderId, 4, "Замовлення зібране та готове до відправки");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int orderId, string trackingNumber = "")
        {
            var order = await _context.Order_lists.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Замовлення не знайдено" });
            }

            // Generate tracking number if not provided
            if (string.IsNullOrEmpty(trackingNumber))
            {
                trackingNumber = GenerateTrackingNumber();
            }

            order.TrackingNumber = trackingNumber;

            var result = await UpdateOrderStatus(orderId, 5, $"Замовлення відправлено. Номер відстеження: {trackingNumber}");

            if (result is JsonResult jsonResult &&
                jsonResult.Value is object value &&
                value.GetType().GetProperty("success")?.GetValue(value)?.Equals(true) == true)
            {
                return Json(new
                {
                    success = true,
                    message = "Замовлення успішно відправлено",
                    trackingNumber = trackingNumber
                });
            }

            return result;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrder(int orderId, string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return Json(new { success = false, message = "Вкажіть причину відхилення" });
            }

            return await UpdateOrderStatus(orderId, 7, $"Замовлення скасовано. Причина: {reason}");
        }

        public async Task<IActionResult> Inventory()
        {
            var inventory = await _context.Accessories
                .Include(a => a.Catalog)
                .OrderBy(a => a.Catalog.Accessory_type)
                .ThenBy(a => a.Accessory_Name)
                .ToListAsync();

            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(int productId, string availability)
        {
            var product = await _context.Accessories.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Товар не знайдено" });
            }

            product.Accessory_Availability = availability;
            await _context.SaveChangesAsync();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Log availability change
            var log = new Log
            {
                Order_id = 0, // Not related to specific order
                User_id = userId,
                Last_Change = DateTime.Now,
                Action = $"Змінено доступність товару '{product.Accessory_Name}' на '{availability}'"
            };

            // Note: This would require modification to Log table to allow Order_id to be optional
            // For now, we'll skip logging inventory changes

            return Json(new { success = true, message = "Доступність товару оновлено" });
        }

        public async Task<IActionResult> PackingList(int orderId)
        {
            var order = await _context.Order_lists
                .Include(o => o.Customer)
                .ThenInclude(c => c.Address)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .ThenInclude(a => a.Catalog)
                .FirstOrDefaultAsync(o => o.Order_id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private async Task<IActionResult> UpdateOrderStatus(int orderId, int statusId, string notes)
        {
            var order = await _context.Order_lists.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Замовлення не знайдено" });
            }

            var oldStatusId = order.Order_status_id;
            order.Order_status_id = statusId;

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Add log entry
            var log = new Log
            {
                Order_id = orderId,
                User_id = userId,
                Last_Change = DateTime.Now,
                Action = notes
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Статус успішно оновлено"
            });
        }

        private string GenerateTrackingNumber()
        {
            return "SHP" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);
        }
    }
}