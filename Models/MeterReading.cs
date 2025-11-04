using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterService.Models
{
    public class MeterReading
    {
        [Key]
        public int Id { get; set; }

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
        public decimal? Consumption => NewIndex - OldIndex;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        [NotMapped]
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// Tính tổng tiền theo công thức bậc thang
        /// Công thức: IF(F7<=30, F7*8470, IF(F7<=60, 30*8470 + (F7-30)*11000, 30*8470 + 30*11000 + (F7-60)*13200))
        /// </summary>
        public decimal CalculateTotalAmount(decimal tier1Price, decimal tier2Price, decimal tier3Price)
        {
            var consumption = Consumption ?? 0;
            
            if (consumption <= 30)
            {
                return consumption * tier1Price;
            }
            else if (consumption <= 60)
            {
                return 30 * tier1Price + (consumption - 30) * tier2Price;
            }
            else
            {
                return 30 * tier1Price + 30 * tier2Price + (consumption - 60) * tier3Price;
            }
        }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int CustomerId { get; set; }
        public required Customer Customer { get; set; }
        public virtual Invoice? Invoice { get; set; }
    }

    public enum QuarterInYear
    {
        [Display(Name = "Quý 1")]
        Q1,
        [Display(Name = "Quý 2")]
        Q2,
        [Display(Name = "Quý 3")]
        Q3,
        [Display(Name = "Quý 4")]
        Q4
    }
}