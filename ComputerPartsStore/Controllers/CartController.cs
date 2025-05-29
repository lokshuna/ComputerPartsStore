using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using ComputerPartsStore.Data;
using ComputerPartsStore.Models;

namespace ComputerPartsStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Accessories.FindAsync(productId);
            if (product == null || product.Accessory_Availability != "В наявності")
            {
                return Json(new { success = false, message = "Товар недоступний" });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.Accessory_id == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Accessory_id = product.Accessory_id,
                    Accessory_Name = product.Accessory_Name,
                    Accessory_Price = product.Accessory_Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return Json(new
            {
                success = true,
                message = "Товар додано до кошика",
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Accessory_id == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartTotal = cart.Sum(c => c.Total),
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Accessory_id == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartTotal = cart.Sum(c => c.Total),
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.User_id == userId);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(c => c.Total),
                Customer = user,
                DeliveryAddress = user.Address
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Create order
            var order = new Order_list
            {
                Customer_id = userId,
                Order_status_id = 1, // New order
                Overlay_id = new Random().Next(100000, 999999),
                Order_Date = DateTime.Now
            };

            _context.Order_lists.Add(order);
            await _context.SaveChangesAsync();

            // Add order items
            foreach (var item in cart)
            {
                var orderItem = new Order_Item
                {
                    Order_id = order.Order_id,
                    Accessory_id = item.Accessory_id,
                    Item_Price = (int)item.Accessory_Price,
                    Item_Count = item.Quantity
                };

                _context.Order_Items.Add(orderItem);
            }

            // Add log entry
            var log = new Log
            {
                Order_id = order.Order_id,
                User_id = userId,
                Last_Change = DateTime.Now,
                Action = "Замовлення створено"
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            // Clear cart
            HttpContext.Session.Remove(CartSessionKey);

            TempData["OrderSuccess"] = $"Замовлення #{order.Overlay_id} успішно оформлено!";
            return RedirectToAction("OrderDetails", new { id = order.Order_id });
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var order = await _context.Order_lists
                .Include(o => o.Order_Status)
                .Include(o => o.Customer)
                .ThenInclude(c => c.Address)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .FirstOrDefaultAsync(o => o.Order_id == id && o.Customer_id == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var orders = await _context.Order_lists
                .Include(o => o.Order_Status)
                .Include(o => o.Order_Items)
                .ThenInclude(oi => oi.Accessories)
                .Where(o => o.Customer_id == userId)
                .OrderByDescending(o => o.Order_Date)
                .ToListAsync();

            return View(orders);
        }

        public int GetCartCount()
        {
            var cart = GetCart();
            return cart.Sum(c => c.Quantity);
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }
    }
}