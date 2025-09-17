using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Customer ID")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Phone number must be 10-11 digits")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual List<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    }
}