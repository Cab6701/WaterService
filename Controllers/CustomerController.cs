using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.Tasks;
using WaterService.Data;
using WaterService.Extensions;
using WaterService.Models;
using WaterService.Services;

namespace WaterService.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;
        private readonly ITierPriceService _tierPriceService;

        public CustomerController(ApplicationDbContext context, IInvoiceService invoiceService, ITierPriceService tierPriceService)
        {
            _context = context;
            _invoiceService = invoiceService;
            _tierPriceService = tierPriceService;
        }

        // GET: Customer
        public async Task<IActionResult> Index(string? search, string? address, int? status, int? quarter, int? year, int page = 1, int pageSize = 20)
        {
            await Task.Yield();
            var query = _context.Customers
                .Include(c => c.MeterReadings)
                .ThenInclude(m => m.Invoice)
                .Include(c => c.Invoices)
                .AsQueryable();

            quarter ??= (DateTime.Now.Month - 1) / 3 + 1;
            year ??= DateTime.Now.Year;

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.CustomerCode.Contains(search) ||
                    c.Name.Contains(search) ||
                    c.PhoneNumber.Contains(search));
            }

            if (address != null)
            {
                var addressName = address;
                query = query.Where(c => c.Address == addressName);
            }

            if (status != null)
            {
                query = query.Where(c => c.Invoices.Any(i => i.Status == (InvoiceStatus)status));
            }

            query = query.Where(c => c.MeterReadings.Any(i => i.Year == year.Value && i.Quarter == quarter.Value));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var customers = await query
                .OrderBy(c => c.CustomerCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var tierPrice = await _tierPriceService.GetCurrentTierPriceAsync();
            tierPrice ??= new TierPrice();

            var viewModel = new CustomerIndexViewModel
            {
                Customers = customers,
                Search = search,
                Address = address == null ? string.Empty : address,
                Status = status == null ? string.Empty : ((InvoiceStatus)status).ToString(),
                Quarter = quarter,
                Year = year,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                TierPriceForm = tierPrice
            };

            return View(viewModel);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.MeterReadings)
                    .ThenInclude(m => m.Invoice)
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Tính toán tóm tắt thanh toán dựa trên hóa đơn thực tế
            var invoices = customer.Invoices ?? new List<Invoice>();
            var totalPaid = invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);
            var totalDue = invoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.TotalAmount);
            var totalInvoices = invoices.Count;
            var overdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);

            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalDue = totalDue;
            ViewBag.TotalInvoices = totalInvoices;
            ViewBag.OverdueCount = overdueCount;

            return View(customer);
        }

        // POST: Customer/EditMeterReadings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeterReadings(int CustomerId, int? Id, int Quarter, int Year, decimal PreviousReading, decimal CurrentReading)
        {
            var meterReading = _context.MeterReadings
                .Include(m => m.Customer)
                .FirstOrDefault(m => m.Id == Id && m.CustomerId == CustomerId);
            if (meterReading == null)
            {
                return NotFound();
            }
            meterReading.Quarter = Quarter;
            meterReading.Year = Year;
            meterReading.OldIndex = PreviousReading;
            meterReading.NewIndex = CurrentReading;
            meterReading.UpdatedAt = DateTime.UtcNow;

            _context.MeterReadings.Update(meterReading);
            await _context.SaveChangesAsync();

            // Tự động tạo/cập nhật Invoice
            await _invoiceService.CreateOrUpdateInvoiceAsync(meterReading);

            TempData["SuccessMessage"] = "Đã cập nhật chỉ số nước và hóa đơn.";
            return RedirectToAction(nameof(Edit), new { id = CustomerId });
        }

        // POST: Customer/AddMeterReadings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeterReadings(int CustomerId, int Quarter, int Year, decimal PreviousReading, decimal CurrentReading)
        {
            var customer = _context.Customers.Find(CustomerId);
            if (customer == null)
            {
                return NotFound();
            }

            var meterReading = new MeterReading
            {
                Quarter = Quarter,
                Year = Year,
                OldIndex = PreviousReading,
                NewIndex = CurrentReading,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MeterReadings.Add(meterReading);
            await _context.SaveChangesAsync();

            // Tự động tạo Invoice
            await _invoiceService.CreateOrUpdateInvoiceAsync(meterReading);

            TempData["SuccessMessage"] = "Đã thêm chỉ số nước và tạo hóa đơn.";
            return RedirectToAction(nameof(Edit), new { id = CustomerId });
        }

        // POST: Customer/DeleteMeterReading
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeterReading(int id, int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
                return NotFound();
            var reading = _context.MeterReadings.FirstOrDefault(r => r.Id == id && r.Customer.Id == customer.Id);
            if (reading == null)
                return NotFound();

            // Xóa Invoice liên kết trước
            await _invoiceService.DeleteInvoiceAsync(id);

            _context.MeterReadings.Remove(reading);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa chỉ số nước và hóa đơn liên kết.";
            return RedirectToAction(nameof(Edit), new { id = customer.Id });
        }

        // POST: Customer/UpdateInvoiceStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInvoiceStatus(int invoiceId, InvoiceStatus status)
        {
            // Chỉ Admin mới được phép cập nhật trạng thái thanh toán
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")) || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật trạng thái thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            invoice.Status = status;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Cập nhật ngày thanh toán nếu trạng thái là Đã thanh toán, ngược lại xóa ngày thanh toán
            invoice.PaidDate = status == InvoiceStatus.Paid ? DateTime.UtcNow : null;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái thanh toán hóa đơn.";
            return RedirectToAction(nameof(Details), new { id = invoice.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTierPrice(TierPrice tierPrice)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")) || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật giá bậc thang.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu giá bậc thang không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index));
            }

            await _tierPriceService.UpdateTierPriceAsync(tierPrice);
            TempData["SuccessMessage"] = "Đã cập nhật giá bậc thang thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, int InitialQuarter, int InitialYear, decimal InitialOldIndex = 0, decimal InitialNewIndex = 0)
        {
            if (ModelState.IsValid)
            {
                customer.CustomerCode = customer.CustomerCode;
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;

                var initialReading = new MeterReading
                {
                    Quarter = InitialQuarter,
                    Year = InitialYear,
                    OldIndex = InitialOldIndex,
                    NewIndex = InitialNewIndex,
                    Customer = customer,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (customer.MeterReadings == null)
                {
                    customer.MeterReadings = new List<MeterReading>();
                }
                customer.MeterReadings.Add(initialReading);
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Tự động tạo Invoice cho chỉ số ban đầu
                await _invoiceService.CreateOrUpdateInvoiceAsync(initialReading);

                TempData["SuccessMessage"] = "Đã tạo khách hàng thành công.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }

            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.MeterReadings)
                    .ThenInclude(m => m.Invoice)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = _context.Customers.Find(customer.Id);
                if (existingCustomer == null)
                {
                    return NotFound();
                }

                existingCustomer.CustomerCode = customer.CustomerCode;
                existingCustomer.Name = customer.Name;
                existingCustomer.Address = customer.Address;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                existingCustomer.Notes = customer.Notes;
                existingCustomer.UpdatedAt = DateTime.UtcNow;

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đã cập nhật thông tin khách hàng thành công.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }

            return View(customer);
        }

        // GET: Customer/Delete/5
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _context.Customers
                .Include(c => c.Invoices)
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (customer.Invoices != null && customer.Invoices.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng đang có hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đã xóa khách hàng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Customer/BulkAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkAction(string action, int[] customerIds)
        {
            if (customerIds == null || customerIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Không có khách hàng nào được chọn!";
                return RedirectToAction(nameof(Index));
            }

            var selectedCustomers = _context.Customers.Where(c => customerIds.Contains(c.Id)).ToList();

            //switch (action.ToLower())
            //{
            //    case "export":
            //        return ExportCustomers(selectedCustomers);
            //    case "activate":
            //        foreach (var customer in selectedCustomers)
            //        {
            //            customer.Status = CustomerStatus.Paid;
            //            customer.UpdatedAt = DateTime.UtcNow;
            //        }
            //        TempData["SuccessMessage"] = $"{selectedCustomers.Count} customers activated.";
            //        break;
            //    case "deactivate":
            //        foreach (var customer in selectedCustomers)
            //        {
            //            customer.Status = CustomerStatus.Pending;
            //            customer.UpdatedAt = DateTime.UtcNow;
            //        }
            //        TempData["SuccessMessage"] = $"{selectedCustomers.Count} customers deactivated.";
            //        break;
            //    default:
            //        TempData["ErrorMessage"] = "Invalid action selected.";
            //        break;
            //}

            return RedirectToAction(nameof(Index));
        }

        private IActionResult ExportCustomers(List<Customer> customers)
        {
            // Simple CSV export
            var csv = "Customer Code,Name,Phone,Address,Status,Registration Date\n";
            foreach (var customer in customers)
            {
                csv += $"{customer.CustomerCode},{customer.Name},{customer.PhoneNumber},{customer.Address}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"customers_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
    public class CustomerIndexViewModel
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public string? Search { get; set; }
        public string? Address { get; set; }
        public string? Status { get; set; }
        public int? Quarter { get; set; }
        public int? Year { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public TierPrice TierPriceForm { get; set; } = new TierPrice();
    }
}

