using Microsoft.AspNetCore.Mvc;
using WaterService.Models;
using System.Linq;

namespace WaterService.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private static List<Customer> _customers = new List<Customer>();
        private static int _nextCustomerId = 1;
        private static int _nextCustomerCode = 1001;
        private static int _nextMeterReadingId = 1;

        public CustomerController(ILogger<CustomerController> logger)
        {
            _logger = logger;
            InitializeSampleData();
        }

        // GET: Customer
        public IActionResult Index(string? search, string? status, int? quarter, int? year, int page = 1, int pageSize = 20)
        {
            var query = _customers.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.CustomerCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.HouseholdHeadName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CustomerStatus>(status, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            // Lọc theo năm/quý dựa trên MeterReadings
            if (year.HasValue)
            {
                if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                {
                    query = query.Where(c => c.MeterReadings != null &&
                        c.MeterReadings.Any(r => r.Year == year.Value && r.Quarter == quarter.Value));
                }
                else
                {
                    query = query.Where(c => c.MeterReadings != null &&
                        c.MeterReadings.Any(r => r.Year == year.Value));
                }
            }

            // Calculate pagination
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
                Status = status,
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
            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // GET: Customer/EditMeterReading
        [HttpGet]
        public IActionResult EditMeterReading(int id, int customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == customerId);
            if (customer == null)
                return NotFound();
            var reading = customer.MeterReadings?.FirstOrDefault(r => r.Id == id);
            if (reading == null)
                return NotFound();
            ViewBag.EditReading = reading;
            return View("Details", customer);
        }

        // POST: Customer/AddOrEditMeterReading
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOrEditMeterReading(int CustomerId, int? Id, int Quarter, int Year, decimal PreviousReading, decimal CurrentReading, string? Notes)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == CustomerId);
            if (customer == null)
                return NotFound();

            MeterReading reading;
            if (customer.MeterReadings == null)
                customer.MeterReadings = new List<MeterReading>();
            if (Id.HasValue && Id.Value > 0)
            {
                // Edit
                reading = customer.MeterReadings.FirstOrDefault(r => r.Id == Id.Value);
                if (reading == null)
                    return NotFound();
                reading.Quarter = Quarter;
                reading.Year = Year;
                reading.PreviousReading = PreviousReading;
                reading.CurrentReading = CurrentReading;
                reading.Notes = Notes;
                reading.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                reading = new MeterReading
                {
                    Id = _nextMeterReadingId++,
                    CustomerId = CustomerId,
                    Quarter = Quarter,
                    Year = Year,
                    PreviousReading = PreviousReading,
                    CurrentReading = CurrentReading,
                    Notes = Notes,
                    CreatedAt = DateTime.UtcNow
                };
                customer.MeterReadings.Add(reading);
            }
            TempData["SuccessMessage"] = "Lưu chỉ số nước thành công.";
            return RedirectToAction("Details", new { id = CustomerId });
        }

        // POST: Customer/DeleteMeterReading
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMeterReading(int id, int customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == customerId);
            if (customer == null)
                return NotFound();
            if (customer.MeterReadings == null)
                return NotFound();
            var reading = customer.MeterReadings.FirstOrDefault(r => r.Id == id);
            if (reading == null)
                return NotFound();
            customer.MeterReadings.Remove(reading);
            TempData["SuccessMessage"] = "Đã xóa chỉ số nước.";
            return RedirectToAction("Details", new { id = customerId });
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            var customer = new Customer
            {
                CustomerCode = $"C{_nextCustomerCode:D6}"
            };
            return View(customer);
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                customer.Id = _nextCustomerId++;
                customer.CustomerCode = $"C{_nextCustomerCode:D6}";
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;

                _customers.Add(customer);
                _nextCustomerCode++;

                TempData["SuccessMessage"] = "Customer created successfully.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }

            return View(customer);
        }

        // GET: Customer/Edit/5
        public IActionResult Edit(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingCustomer = _customers.FirstOrDefault(c => c.Id == id);
                if (existingCustomer == null)
                {
                    return NotFound();
                }

                existingCustomer.HouseholdHeadName = customer.HouseholdHeadName;
                existingCustomer.Address = customer.Address;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                existingCustomer.Status = customer.Status;
                existingCustomer.Notes = customer.Notes;
                existingCustomer.UpdatedAt = DateTime.UtcNow;

                TempData["SuccessMessage"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }

            return View(customer);
        }

        // GET: Customer/Delete/5
        public IActionResult Delete(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == id);
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
            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            // Check if customer has any invoices
            if (customer.Invoices != null && customer.Invoices.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete customer with existing invoices.";
                return RedirectToAction(nameof(Index));
            }

            _customers.Remove(customer);
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
                TempData["ErrorMessage"] = "No customers selected.";
                return RedirectToAction(nameof(Index));
            }

            var selectedCustomers = _customers.Where(c => customerIds.Contains(c.Id)).ToList();

            switch (action.ToLower())
            {
                case "export":
                    return ExportCustomers(selectedCustomers);
                case "activate":
                    foreach (var customer in selectedCustomers)
                    {
                        customer.Status = CustomerStatus.Paid;
                        customer.UpdatedAt = DateTime.UtcNow;
                    }
                    TempData["SuccessMessage"] = $"{selectedCustomers.Count} customers activated.";
                    break;
                case "deactivate":
                    foreach (var customer in selectedCustomers)
                    {
                        customer.Status = CustomerStatus.Pending;
                        customer.UpdatedAt = DateTime.UtcNow;
                    }
                    TempData["SuccessMessage"] = $"{selectedCustomers.Count} customers deactivated.";
                    break;
                default:
                    TempData["ErrorMessage"] = "Invalid action selected.";
                    break;
            }

            return RedirectToAction(nameof(Index));
        }

        private IActionResult ExportCustomers(List<Customer> customers)
        {
            // Simple CSV export
            var csv = "Customer Code,Name,Phone,Address,Status,Registration Date\n";
            foreach (var customer in customers)
            {
                csv += $"{customer.CustomerCode},{customer.HouseholdHeadName},{customer.PhoneNumber},{customer.Address},{customer.Status}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"customers_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private void InitializeSampleData()
        {
            if (_customers.Any()) return;

            var sampleCustomers = new List<Customer>
            {
                new Customer
                {
                    Id = _nextCustomerId++,
                    CustomerCode = $"C{_nextCustomerCode++:D6}",
                    HouseholdHeadName = "Nguyen Van A",
                    Address = "123 Main Street, District 1, HCMC",
                    PhoneNumber = "0901234567",
                    Status = CustomerStatus.Paid,
                    Notes = "Regular customer",
                    CreatedAt = DateTime.UtcNow.AddDays(-365),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30),
                    MeterReadings = new List<MeterReading>
                    {
                        new MeterReading
                        {
                            Id = _nextMeterReadingId++,
                            CustomerId = 1,
                            Quarter = 1,
                            Year = 2023,
                            PreviousReading = 100,
                            CurrentReading = 120,
                            Notes = "Q1/2023",
                            CreatedAt = DateTime.UtcNow.AddDays(-360)
                        }
                    }
                },
                new Customer
                {
                    Id = _nextCustomerId++,
                    CustomerCode = $"C{_nextCustomerCode++:D6}",
                    HouseholdHeadName = "Tran Thi B",
                    Address = "456 Second Street, District 2, HCMC",
                    PhoneNumber = "0901234568",
                    Status = CustomerStatus.Paid,
                    Notes = "Commercial customer",
                    CreatedAt = DateTime.UtcNow.AddDays(-300),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15),
                    MeterReadings = new List<MeterReading>
                    {
                        new MeterReading
                        {
                            Id = _nextMeterReadingId++,
                            CustomerId = 2,
                            Quarter = 2,
                            Year = 2023,
                            PreviousReading = 200,
                            CurrentReading = 230,
                            Notes = "Q2/2023",
                            CreatedAt = DateTime.UtcNow.AddDays(-290)
                        }
                    }
                },
                new Customer
                {
                    Id = _nextCustomerId++,
                    CustomerCode = $"C{_nextCustomerCode++:D6}",
                    HouseholdHeadName = "Le Van C",
                    Address = "789 Third Street, District 3, HCMC",
                    PhoneNumber = "0901234569",
                    Status = CustomerStatus.Pending,
                    Notes = "Temporarily inactive",
                    CreatedAt = DateTime.UtcNow.AddDays(-200),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    MeterReadings = new List<MeterReading>()
                }
            };

            _customers.AddRange(sampleCustomers);
        }
    }

    public class CustomerIndexViewModel
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? Quarter { get; set; }
        public int? Year { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }
}