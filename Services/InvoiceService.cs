using Microsoft.EntityFrameworkCore;
using WaterService.Data;
using WaterService.Models;

namespace WaterService.Services
{
    public interface IInvoiceService
    {
        Task<Invoice> CreateOrUpdateInvoiceAsync(MeterReading meterReading, TierPrice? tierPrice = null);
        Task DeleteInvoiceAsync(int meterReadingId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITierPriceService _tierPriceService;

        public InvoiceService(ApplicationDbContext context, ITierPriceService tierPriceService)
        {
            _context = context;
            _tierPriceService = tierPriceService;
        }

        public async Task<Invoice> CreateOrUpdateInvoiceAsync(MeterReading meterReading, TierPrice? tierPrice = null)
        {
            // Lấy giá bậc thang nếu chưa được truyền vào
            if (tierPrice == null)
            {
                tierPrice = await _tierPriceService.GetCurrentTierPriceAsync();
            }

            // Tìm Invoice hiện tại liên kết với MeterReading này
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.MeterReadingId == meterReading.Id);

            Invoice invoice;

            if (existingInvoice != null)
            {
                // Cập nhật Invoice hiện tại
                invoice = existingInvoice;
                // Dùng giá bậc thang đã áp dụng cho hóa đơn (nếu có), để không thay đổi khi giá mới cập nhật
                TierPrice? appliedTier = null;
                if (invoice.AppliedTierPriceId.HasValue)
                {
                    appliedTier = await _context.TierPrices.FindAsync(invoice.AppliedTierPriceId.Value);
                }
                appliedTier ??= tierPrice; // fallback an toàn

                var recalculated = meterReading.CalculateTotalAmount(
                    appliedTier!.Tier1Price,
                    appliedTier.Tier2Price,
                    appliedTier.Tier3Price);

                meterReading.TotalAmount = recalculated;
                invoice.TotalAmount = recalculated;
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
                    // Áp dụng giá hiện tại tại thời điểm tạo hóa đơn
                    TotalAmount = meterReading.CalculateTotalAmount(
                        tierPrice.Tier1Price,
                        tierPrice.Tier2Price,
                        tierPrice.Tier3Price),
                    AppliedTierPriceId = tierPrice.Id,
                    MeterReadingId = meterReading.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                meterReading.TotalAmount = invoice.TotalAmount;

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
