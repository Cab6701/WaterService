using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class MeterReading
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, 4)]
        [Display(Name = "Quarter")]
        public int Quarter { get; set; }

        [Required]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Previous Reading")]
        public decimal PreviousReading { get; set; }

        [Required]
        [Display(Name = "Current Reading")]
        public decimal CurrentReading { get; set; }

        [Display(Name = "Consumption")]
        public decimal Consumption => CurrentReading - PreviousReading;

        [Display(Name = "Rate per Unit")]
        [DataType(DataType.Currency)]
        public decimal RatePerUnit { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => Consumption * RatePerUnit;

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
    }
}