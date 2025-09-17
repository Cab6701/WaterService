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
        [Display(Name = "Old Index")]
        public decimal OldIndex { get; set; }

        [Required]
        [Display(Name = "New Index")]
        public decimal NewIndex { get; set; }

        [Display(Name = "Consumption")]
        public decimal Consumption => NewIndex - OldIndex;

        [Display(Name = "Rate per Unit")]
        [DataType(DataType.Currency)]
        public decimal RatePerUnit { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => Consumption * RatePerUnit;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}