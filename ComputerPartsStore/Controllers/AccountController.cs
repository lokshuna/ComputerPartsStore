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

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
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

                    // Redirect based on role
                    if (user.Role_Name == "Operator")
                    {
                        return RedirectToAction("Index", "Operator");
                    }
                    else if (user.Role_Name == "Storekeeper")
                    {
                        return RedirectToAction("Index", "Storekeeper");
                    }
                    else
                    {
                        return RedirectToLocal(returnUrl);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.User_login == model.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Користувач з таким логіном вже існує.");
                    return View(model);
                }

                // Create address
                var address = new Address
                {
                    City = model.City,
                    Region = model.Region,
                    House_Number = model.HouseNumber
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                // Create user
                var user = new User
                {
                    User_login = model.Login,
                    User_password = model.Password,
                    Name = model.Name,
                    Second_Name = model.SecondName,
                    Patronymic = model.Patronymic,
                    Phone_Number = model.PhoneNumber,
                    Role_Name = "Customer",
                    Address_id = address.Address_id
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Auto login after registration
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

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.User_id == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}