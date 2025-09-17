using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Water Meter Reading")]
        public int? WaterMeterReadingId { get; set; }

        [Display(Name = "Status")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Paid Date")]
        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Overdue,
        Cancelled
    }
}