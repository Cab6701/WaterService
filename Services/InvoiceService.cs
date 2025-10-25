using Microsoft.EntityFrameworkCore;
using WaterService.Data;
using WaterService.Models;

namespace WaterService.Services
{
    public interface IInvoiceService
    {
        Task<Invoice> CreateOrUpdateInvoiceAsync(MeterReading meterReading);
        Task DeleteInvoiceAsync(int meterReadingId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> CreateOrUpdateInvoiceAsync(MeterReading meterReading)
        {
            // Tìm Invoice hiện tại liên kết với MeterReading này
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.MeterReadingId == meterReading.Id);

            Invoice invoice;

            if (existingInvoice != null)
            {
                // Cập nhật Invoice hiện tại
                invoice = existingInvoice;
                invoice.TotalAmount = meterReading.TotalAmount ?? 0;
                invoice.UpdatedAt = DateTime.UtcNow;
                
                // Nếu Invoice đã được thanh toán, không thay đổi trạng thái
                if (invoice.Status != InvoiceStatus.Paid)
                {
                    invoice.Status = InvoiceStatus.Pending;
                }
                
                _context.Invoices.Update(invoice);
            }
            else
            {
                // Tạo Invoice mới
                invoice = new Invoice
                {
                    CustomerCode = meterReading.Customer.CustomerCode,
                    CustomerId = meterReading.Customer.Id,
                    InvoiceNumber = GenerateInvoiceNumber(meterReading),
                    Status = InvoiceStatus.Pending,
                    DueDate = CalculateDueDate(meterReading.Year, meterReading.Quarter),
                    TotalAmount = meterReading.TotalAmount ?? 0,
                    MeterReadingId = meterReading.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
            }

            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task DeleteInvoiceAsync(int meterReadingId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.MeterReadingId == meterReadingId);

            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateInvoiceNumber(MeterReading meterReading)
        {
            // Format: INV-YYYY-QX-XXXXXX (VD: INV-2024-Q1-000001)
            var quarterPrefix = $"Q{meterReading.Quarter}";
            var year = meterReading.Year;
            var customerCode = meterReading.Customer.CustomerCode.Replace("C", "").PadLeft(6, '0');
            
            return $"INV-{year}-{quarterPrefix}-{customerCode}";
        }

        private DateTime CalculateDueDate(int year, int quarter)
        {
            // Tính ngày đến hạn dựa trên quý
            var dueDate = quarter switch
            {
                1 => new DateTime(year, 4, 30), // Q1 đến hạn cuối tháng 4
                2 => new DateTime(year, 7, 31), // Q2 đến hạn cuối tháng 7
                3 => new DateTime(year, 10, 31), // Q3 đến hạn cuối tháng 10
                4 => new DateTime(year + 1, 1, 31), // Q4 đến hạn cuối tháng 1 năm sau
                _ => DateTime.Now.AddDays(30) // Mặc định 30 ngày
            };

            return dueDate;
        }
    }
}
