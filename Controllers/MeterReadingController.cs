using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterService.Data;
using WaterService.Models;
using WaterService.Services;

namespace WaterService.Controllers
{
    public class MeterReadingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public MeterReadingController(ApplicationDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        // GET: MeterReading
        public IActionResult Index(int? customerId, int? quarter, int? year, int page = 1, int pageSize = 20)
        {
            var query = _context.MeterReadings
                .Include(m => m.Customer)
                .Include(m => m.Invoice)
                .AsQueryable();

            if (customerId.HasValue)
            {
                query = query.Where(m => m.CustomerId == customerId.Value);
            }

            if (quarter.HasValue)
            {
                query = query.Where(m => m.Quarter == quarter.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(m => m.Year == year.Value);
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var meterReadings = query
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Quarter)
                .ThenBy(m => m.Customer.CustomerCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var customers = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
            ViewBag.Customers = customers;

            var viewModel = new MeterReadingIndexViewModel
            {
                MeterReadings = meterReadings,
                CustomerId = customerId,
                Quarter = quarter,
                Year = year,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: MeterReading/Details/5
        public IActionResult Details(int id)
        {
            var meterReading = _context.MeterReadings
                .Include(m => m.Customer)
                .Include(m => m.Invoice)
                .FirstOrDefault(m => m.Id == id);

            if (meterReading == null)
            {
                return NotFound();
            }

            return View(meterReading);
        }

        // GET: MeterReading/Create
        public IActionResult Create(int? customerId)
        {
            var customers = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
            ViewBag.Customers = customers;
            ViewBag.CustomerId = customerId;

            var meterReading = new MeterReading
            {
                CustomerId = customerId ?? 0,
                Year = DateTime.Now.Year,
                Quarter = 1,
                Customer = new Customer() // Temporary customer for form binding
            };
            return View(meterReading);
        }

        // POST: MeterReading/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MeterReading meterReading)
        {
            if (ModelState.IsValid)
            {
                var customer = await _context.Customers.FindAsync(meterReading.CustomerId);
                if (customer == null)
                {
                    ModelState.AddModelError("CustomerId", "Khách hàng không tồn tại.");
                    var customers = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
                    ViewBag.Customers = customers;
                    return View(meterReading);
                }

                meterReading.Customer = customer;
                meterReading.CreatedAt = DateTime.UtcNow;
                meterReading.UpdatedAt = DateTime.UtcNow;

                _context.MeterReadings.Add(meterReading);
                await _context.SaveChangesAsync();

                // Tự động tạo Invoice
                await _invoiceService.CreateOrUpdateInvoiceAsync(meterReading);

                TempData["SuccessMessage"] = "Đã tạo chỉ số nước và hóa đơn thành công.";
                return RedirectToAction(nameof(Details), new { id = meterReading.Id });
            }

            var customersList = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
            ViewBag.Customers = customersList;
            return View(meterReading);
        }

        // GET: MeterReading/Edit/5
        public IActionResult Edit(int id)
        {
            var meterReading = _context.MeterReadings
                .Include(m => m.Customer)
                .FirstOrDefault(m => m.Id == id);

            if (meterReading == null)
            {
                return NotFound();
            }

            var customers = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
            ViewBag.Customers = customers;

            return View(meterReading);
        }

        // POST: MeterReading/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MeterReading meterReading)
        {
            if (id != meterReading.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    meterReading.UpdatedAt = DateTime.UtcNow;
                    _context.MeterReadings.Update(meterReading);
                    await _context.SaveChangesAsync();

                    // Tự động cập nhật Invoice
                    await _invoiceService.CreateOrUpdateInvoiceAsync(meterReading);

                    TempData["SuccessMessage"] = "Đã cập nhật chỉ số nước và hóa đơn thành công.";
                    return RedirectToAction(nameof(Details), new { id = meterReading.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeterReadingExists(meterReading.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var customers = _context.Customers.OrderBy(c => c.CustomerCode).ToList();
            ViewBag.Customers = customers;
            return View(meterReading);
        }

        // GET: MeterReading/Delete/5
        public IActionResult Delete(int id)
        {
            var meterReading = _context.MeterReadings
                .Include(m => m.Customer)
                .Include(m => m.Invoice)
                .FirstOrDefault(m => m.Id == id);

            if (meterReading == null)
            {
                return NotFound();
            }

            return View(meterReading);
        }

        // POST: MeterReading/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meterReading = _context.MeterReadings.Find(id);
            if (meterReading == null)
            {
                return NotFound();
            }

            // Xóa Invoice liên kết trước
            await _invoiceService.DeleteInvoiceAsync(id);

            _context.MeterReadings.Remove(meterReading);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa chỉ số nước và hóa đơn liên kết thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool MeterReadingExists(int id)
        {
            return _context.MeterReadings.Any(e => e.Id == id);
        }
    }

    public class MeterReadingIndexViewModel
    {
        public List<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
        public int? CustomerId { get; set; }
        public int? Quarter { get; set; }
        public int? Year { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }
}
