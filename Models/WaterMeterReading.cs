using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class WaterMeterReading
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Quarter { get; set; } // 1, 2, 3, 4

        [Required]
        [Display(Name = "Chỉ số cũ")]
        public decimal PreviousReading { get; set; }

        [Required]
        [Display(Name = "Chỉ số mới")]
        public decimal CurrentReading { get; set; }

        [Display(Name = "Tiêu thụ")]
        public decimal Consumption => CurrentReading - PreviousReading;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
