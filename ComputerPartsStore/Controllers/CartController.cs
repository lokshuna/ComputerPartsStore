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
            try
            {
                var product = await _context.Accessories.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Товар не знайдено" });
                }

                if (product.Accessory_Availability != "В наявності")
                {
                    return Json(new { success = false, message = "Товар недоступний для замовлення" });
                }

                if (quantity < 1 || quantity > 10)
                {
                    return Json(new { success = false, message = "Неправильна кількість товару" });
                }

                var cart = GetCart();
                var existingItem = cart.FirstOrDefault(c => c.Accessory_id == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    // Перевіряємо максимальну кількість
                    if (existingItem.Quantity > 10)
                    {
                        existingItem.Quantity = 10;
                        SaveCart(cart);
                        return Json(new
                        {
                            success = true,
                            message = "Товар додано до кошика (максимум 10 шт.)",
                            cartCount = cart.Sum(c => c.Quantity)
                        });
                    }
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
                    message = $"Товар додано до кошика ({quantity} шт.)",
                    cartCount = cart.Sum(c => c.Quantity)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Помилка при додаванні товару" });
            }
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                var cart = GetCart();
                var count = cart.Sum(c => c.Quantity);
                return Json(count);
            }
            catch
            {
                return Json(0);
            }
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(c => c.Accessory_id == productId);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Remove(item);
                    }
                    else if (quantity > 10)
                    {
                        item.Quantity = 10;
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
            catch
            {
                return Json(new { success = false, message = "Помилка при оновленні кошика" });
            }
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            try
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
            catch
            {
                return Json(new { success = false, message = "Помилка при видаленні товару" });
            }
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Ваш кошик порожній";
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.User_id == userId);

            if (user == null)
            {
                TempData["Error"] = "Користувача не знайдено";
                return RedirectToAction("Login", "Account");
            }

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
            try
            {
                var cart = GetCart();
                if (!cart.Any())
                {
                    TempData["Error"] = "Ваш кошик порожній";
                    return RedirectToAction("Index");
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Перевіряємо доступність всіх товарів
                foreach (var cartItem in cart)
                {
                    var product = await _context.Accessories.FindAsync(cartItem.Accessory_id);
                    if (product == null || product.Accessory_Availability != "В наявності")
                    {
                        TempData["Error"] = $"Товар '{cartItem.Accessory_Name}' більше недоступний";
                        return RedirectToAction("Index");
                    }
                }

                // Створюємо замовлення
                var order = new Order_list
                {
                    Customer_id = userId,
                    Order_status_id = 1, // Нове замовлення
                    Overlay_id = new Random().Next(100000, 999999),
                    Order_Date = DateTime.Now,
                    TrackingNumber = ""
                };

                _context.Order_lists.Add(order);
                await _context.SaveChangesAsync();

                // Додаємо товари до замовлення
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

                // Додаємо запис в лог
                var log = new Log
                {
                    Order_id = order.Order_id,
                    User_id = userId,
                    Last_Change = DateTime.Now,
                    Action = "Замовлення створено клієнтом"
                };

                _context.Logs.Add(log);
                await _context.SaveChangesAsync();

                // Очищаємо кошик
                HttpContext.Session.Remove(CartSessionKey);

                TempData["OrderSuccess"] = $"Замовлення #{order.Overlay_id} успішно оформлено! Очікуйте дзвінка оператора.";
                return RedirectToAction("OrderDetails", new { id = order.Order_id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Помилка при оформленні замовлення. Спробуйте ще раз.";
                return RedirectToAction("Checkout");
            }
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
                TempData["Error"] = "Замовлення не знайдено";
                return RedirectToAction("MyOrders");
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

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                // Якщо не вдалося десеріалізувати, повертаємо порожній кошик
                HttpContext.Session.Remove(CartSessionKey);
                return new List<CartItem>();
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            try
            {
                var cartJson = JsonSerializer.Serialize(cart);
                HttpContext.Session.SetString(CartSessionKey, cartJson);
            }
            catch
            {
                // У випадку помилки очищаємо сесію
                HttpContext.Session.Remove(CartSessionKey);
            }
        }
    }
}