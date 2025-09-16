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
        [Display(Name = "Household Head Name")]
        public string HouseholdHeadName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Phone number must be 10-11 digits")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Status")]
        public CustomerStatus Status { get; set; } = CustomerStatus.Paid;

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "New Index")]
        public decimal? NewIndex { get; set; }

        [Display(Name = "Old Index")]
        public decimal? OldIndex { get; set; }

        [Display(Name = "Actual Index")]
        public decimal? ActualIndex { get; set; }

        [Display(Name = "Money")]
        public decimal? Money { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual List<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    }

    public enum CustomerStatus
    {
        Paid,
        Pending,
        Abandoned
    }
}