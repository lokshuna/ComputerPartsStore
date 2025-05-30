using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ComputerPartsStore.Data;
using ComputerPartsStore.Models;

namespace ComputerPartsStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Простий пошук користувача (в реальному проекті пароль має бути захешованим)
                var user = await _context.Users
                    .Include(u => u.Address)
                    .FirstOrDefaultAsync(u => u.User_login == model.Login && u.User_password == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.User_id.ToString()),
                        new Claim(ClaimTypes.Name, user.User_login),
                        new Claim("FullName", $"{user.Name} {user.Second_Name}"),
                        new Claim("Role", user.Role_Name)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    _logger.LogInformation($"User {user.User_login} logged in successfully");

                    // Перенаправлення залежно від ролі
                    if (user.Role_Name == "Operator")
                    {
                        TempData["Success"] = "Ласкаво просимо до панелі оператора!";
                        return RedirectToAction("Index", "Operator");
                    }
                    else if (user.Role_Name == "Storekeeper")
                    {
                        TempData["Success"] = "Ласкаво просимо до панелі комірника!";
                        return RedirectToAction("Index", "Storekeeper");
                    }
                    else
                    {
                        TempData["Success"] = $"Ласкаво просимо, {user.Name}!";
                        return RedirectToLocal(returnUrl);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
                    _logger.LogWarning($"Failed login attempt for user: {model.Login}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                ModelState.AddModelError(string.Empty, "Помилка при вході в систему. Спробуйте ще раз.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Перевіряємо, чи не існує користувач з таким логіном
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.User_login == model.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Користувач з таким логіном вже існує.");
                    return View(model);
                }

                // Валідація даних
                if (model.PhoneNumber.ToString().Length != 12 || !model.PhoneNumber.ToString().StartsWith("380"))
                {
                    ModelState.AddModelError("PhoneNumber", "Телефон має бути у форматі 380XXXXXXXXX");
                    return View(model);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Створюємо адресу
                    var address = new Address
                    {
                        City = model.City.Trim(),
                        Region = model.Region.Trim(),
                        House_Number = model.HouseNumber
                    };

                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();

                    // Створюємо користувача
                    var user = new User
                    {
                        User_login = model.Login.Trim(),
                        User_password = model.Password, // В реальному проекті треба хешувати!
                        Name = model.Name.Trim(),
                        Second_Name = model.SecondName.Trim(),
                        Patronymic = model.Patronymic?.Trim() ?? "",
                        Phone_Number = model.PhoneNumber,
                        Role_Name = "Customer",
                        Address_id = address.Address_id
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"New user registered: {user.User_login}");

                    // Автоматичний вхід після реєстрації
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.User_id.ToString()),
                        new Claim(ClaimTypes.Name, user.User_login),
                        new Claim("FullName", $"{user.Name} {user.Second_Name}"),
                        new Claim("Role", user.Role_Name)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    TempData["Success"] = $"Ласкаво просимо, {user.Name}! Ваш акаунт успішно створено.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                ModelState.AddModelError(string.Empty, "Помилка при реєстрації. Спробуйте ще раз.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation($"User {userName} logged out");

                TempData["Success"] = "Ви успішно вийшли з системи.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = await _context.Users
                    .Include(u => u.Address)
                    .FirstOrDefaultAsync(u => u.User_id == userId);

                if (user == null)
                {
                    TempData["Error"] = "Користувача не знайдено.";
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["Error"] = "Помилка при завантаженні профілю.";
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // Методи для швидкого входу в демо-режимі (тільки для розробки!)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickLogin(string role)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var (login, password) = role.ToLower() switch
                {
                    "operator" => ("operator", "operator123"),
                    "storekeeper" => ("storekeeper", "store123"),
                    _ => (null, null)
                };

                if (login == null)
                {
                    return BadRequest();
                }

                var model = new LoginViewModel
                {
                    Login = login,
                    Password = password,
                    RememberMe = false
                };

                return await Login(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during quick login");
                return RedirectToAction("Login");
            }
        }
    }
}