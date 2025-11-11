using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterService.Data;
using WaterService.Models;
using System.Security.Cryptography;
using System.Text;

namespace WaterService.Controllers
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
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

                if (user == null || !VerifyPassword(model.Password, user.Password))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }

                // Lưu thông tin user vào session
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("DisplayName", user.DisplayName);
                HttpContext.Session.SetString("Role", user.Role.ToString());

                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                // Redirect dựa trên role
                if (user.Role == UserRole.Admin)
                {
                    return RedirectToAction("Index", "Customer");
                }
                else
                {
                    return RedirectToAction("WaterInquiry", "Account");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult WaterInquiry()
        {
            var model = new WaterInquiryViewModel
            {
                Year = DateTime.Now.Year
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WaterInquiry(WaterInquiryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var customer = await _context.Customers
                    .Include(c => c.Invoices)
                    .ThenInclude(i => i.WaterMeterReading)
                    .FirstOrDefaultAsync(c => c.PhoneNumber == model.PhoneNumber);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found with phone number: {PhoneNumber}", model.PhoneNumber);
                    ModelState.AddModelError(string.Empty, $"Không tìm thấy khách hàng với số điện thoại '{model.PhoneNumber}'. Vui lòng kiểm tra lại số điện thoại.");
                    return View(model);
                }

                // Lấy dữ liệu hóa đơn theo năm và quý của MeterReading, bao gồm AppliedTierPrice
                var invoices = await _context.Invoices
                    .Include(i => i.WaterMeterReading)
                    .Include(i => i.AppliedTierPrice)
                    .Where(i => i.CustomerId == customer.Id &&
                               i.WaterMeterReading != null &&
                               i.WaterMeterReading.Year == model.Year &&
                               i.WaterMeterReading.Quarter == model.Quarter)
                    .OrderBy(i => i.WaterMeterReading!.UpdatedAt)
                    .ToListAsync();

                // Lấy invoice đầu tiên để hiển thị thông tin (nếu có)
                var firstInvoice = invoices.FirstOrDefault();
                var meterReading = firstInvoice?.WaterMeterReading;
                var tierPrice = firstInvoice?.AppliedTierPrice;

                // Tính breakdown theo bậc giá
                var tierBreakdown = new List<dynamic>();
                if (meterReading != null && tierPrice != null)
                {
                    var consumption = meterReading.Consumption ?? 0;
                    decimal tier1Consumption = 0, tier2Consumption = 0, tier3Consumption = 0;
                    decimal tier1Amount = 0, tier2Amount = 0, tier3Amount = 0;

                    if (consumption <= 30)
                    {
                        tier1Consumption = consumption;
                        tier1Amount = tier1Consumption * tierPrice.Tier1Price;
                    }
                    else if (consumption <= 60)
                    {
                        tier1Consumption = 30;
                        tier1Amount = tier1Consumption * tierPrice.Tier1Price;
                        tier2Consumption = consumption - 30;
                        tier2Amount = tier2Consumption * tierPrice.Tier2Price;
                    }
                    else
                    {
                        tier1Consumption = 30;
                        tier1Amount = tier1Consumption * tierPrice.Tier1Price;
                        tier2Consumption = 30;
                        tier2Amount = tier2Consumption * tierPrice.Tier2Price;
                        tier3Consumption = consumption - 60;
                        tier3Amount = tier3Consumption * tierPrice.Tier3Price;
                    }

                    tierBreakdown.Add(new
                    {
                        Tier = 1,
                        UnitPrice = tierPrice.Tier1Price,
                        Consumption = tier1Consumption,
                        Amount = tier1Amount
                    });
                    tierBreakdown.Add(new
                    {
                        Tier = 2,
                        UnitPrice = tierPrice.Tier2Price,
                        Consumption = tier2Consumption,
                        Amount = tier2Amount
                    });
                    tierBreakdown.Add(new
                    {
                        Tier = 3,
                        UnitPrice = tierPrice.Tier3Price,
                        Consumption = tier3Consumption,
                        Amount = tier3Amount
                    });
                }

                ViewBag.Customer = customer;
                ViewBag.Invoices = invoices;
                ViewBag.MeterReading = meterReading;
                ViewBag.TierPrice = tierPrice;
                ViewBag.TierBreakdown = tierBreakdown;
                ViewBag.Year = model.Year;
                ViewBag.Quarter = model.Quarter;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during water inquiry for phone number {PhoneNumber}", model.PhoneNumber);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình tra cứu. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SeedData()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Instructions()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeedUsers()
        {
            try
            {
                // Kiểm tra xem đã có user nào chưa
                if (await _context.Users.AnyAsync())
                {
                    return Json(new { success = false, message = "Dữ liệu đã tồn tại" });
                }

                // Tạo admin user
                var adminUser = new User
                {
                    Username = "admin",
                    Password = HashPassword("admin123"),
                    DisplayName = "Quản trị viên",
                    Role = UserRole.Admin,
                    IsActive = true
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã tạo dữ liệu mẫu thành công",
                    users = new[] {
                        new { username = "admin", password = "admin123", role = "Admin" }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                return Json(new
                {
                    success = true,
                    count = users.Count,
                    users = users.Select(u => new
                    {
                        username = u.Username,
                        displayName = u.DisplayName,
                        role = u.Role.ToString(),
                        isActive = u.IsActive
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SeedCustomers()
        {
            try
            {
                // Kiểm tra xem đã có khách hàng nào chưa
                if (await _context.Customers.AnyAsync())
                {
                    return Json(new { success = false, message = "Dữ liệu khách hàng đã tồn tại" });
                }

                // Tạo khách hàng mẫu
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "KH001",
                        Name = "Nguyễn Văn An",
                        Address = "Thôn 1, Xã Minh Sơn",
                        PhoneNumber = "0123456789",
                        Notes = "Khách hàng mẫu 1",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Customer
                    {
                        CustomerCode = "KH002",
                        Name = "Trần Thị Bình",
                        Address = "Thôn 2, Xã Minh Sơn",
                        PhoneNumber = "0987654321",
                        Notes = "Khách hàng mẫu 2",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Customer
                    {
                        CustomerCode = "KH003",
                        Name = "Lê Văn Cường",
                        Address = "Thôn 3, Xã Minh Sơn",
                        PhoneNumber = "0369258147",
                        Notes = "Khách hàng mẫu 3",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Customers.AddRange(customers);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã tạo dữ liệu khách hàng mẫu thành công",
                    customers = customers.Select(c => new
                    {
                        code = c.CustomerCode,
                        name = c.Name
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        private int GetQuarter(DateTime date)
        {
            return (date.Month - 1) / 3 + 1;
        }
    }
}
