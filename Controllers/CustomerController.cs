using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;
using WaterService.Data;
using WaterService.Extensions;
using WaterService.Models;

namespace WaterService.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customer
        public IActionResult Index(string? search, string? address, int? status, int? quarter, int? year, int page = 1, int pageSize = 20)
        {
            var query = _context.Customers
                .Include(c => c.MeterReadings)
                .Include(c => c.Invoices)
                .AsQueryable();

            quarter ??= (DateTime.Now.Month - 1) / 3;
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

            query = query.Where(c => c.MeterReadings.Any(i => i.Year == year.Value && i.Quarter == quarter));

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var customers = query
                .OrderBy(c => c.CustomerCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Customer/Details/5
        public IActionResult Details(int id)
        {
            var customer = _context.Customers
                .Include(c => c.MeterReadings)
                .Include(c => c.Invoices)
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/EditMeterReadings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMeterReadings(int CustomerId, int? Id, int Quarter, int Year, decimal PreviousReading, decimal CurrentReading, decimal UnitPrice)
        {
            var meterReading = _context.MeterReadings.FirstOrDefault(m => m.Id == Id && m.CustomerId == CustomerId);
            if (meterReading == null)
            {
                return NotFound();
            }
            meterReading.Quarter = Quarter - 1;
            meterReading.Year = Year;
            meterReading.OldIndex = PreviousReading;
            meterReading.NewIndex = CurrentReading;
            meterReading.UnitPrice = UnitPrice;
            meterReading.UpdatedAt = DateTime.UtcNow;

            _context.MeterReadings.Update(meterReading);
            _context.SaveChanges();
            return RedirectToAction(nameof(Edit), new { id = CustomerId });
        }

        // POST: Customer/AddMeterReadings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMeterReadings(int CustomerId, int Quarter, int Year, decimal PreviousReading, decimal CurrentReading, decimal UnitPrice)
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
                UnitPrice = UnitPrice,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MeterReadings.Update(meterReading);
            _context.SaveChanges();
            return RedirectToAction(nameof(Edit), new { id = CustomerId });
        }

        // POST: Customer/DeleteMeterReading
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMeterReading(int id, int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
                return NotFound();
            var reading = _context.MeterReadings.FirstOrDefault(r => r.Id == id && r.Customer.Id == customer.Id);
            if (reading == null)
                return NotFound();
            _context.MeterReadings.Remove(reading);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa chỉ số nước.";
            return RedirectToAction(nameof(Edit), new { id = customer.Id });
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer customer, int InitialQuarter, int InitialYear, decimal InitialOldIndex = 0, decimal InitialNewIndex = 0, decimal UnitPrice = 0)
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
                    UnitPrice = UnitPrice,
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
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Customer created successfully.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }

            return View(customer);
        }

        // GET: Customer/Edit/5
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers
                .Include(c => c.MeterReadings)
                .FirstOrDefault(c => c.Id == id);

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

                TempData["SuccessMessage"] = "Customer updated successfully.";
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
                TempData["ErrorMessage"] = "Cannot delete customer with existing invoices.";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Customer deleted successfully.";
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
    }
}

