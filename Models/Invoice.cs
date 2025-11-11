using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer Code")]
        public required string CustomerCode { get; set; }

        public int CustomerId { get; set; }

        // Navigation properties
        public virtual Customer? Customer { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public InvoiceStatus? Status { get; set; } = null;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Paid Date")]
        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        // Giá bậc thang áp dụng cho hóa đơn này (để giữ nguyên giá lịch sử)
        public int? AppliedTierPriceId { get; set; }
        public virtual TierPrice? AppliedTierPrice { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? MeterReadingId { get; set; }
        public virtual MeterReading? WaterMeterReading { get; set; } = null!;
    }

    public enum InvoiceStatus
    {
        [Display(Name = "Đang Chờ")]
        Pending,
        [Display(Name = "Đã thanh toán")]
        Paid,
        [Display(Name = "Quá hạn")]
        Overdue,
        [Display(Name = "Đã hủy")]
        Cancelled
    }
}