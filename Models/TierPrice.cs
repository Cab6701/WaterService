using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class TierPrice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Giá bậc 1 (≤ 30 m³)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [DataType(DataType.Currency)]
        public decimal Tier1Price { get; set; } = 8470;

        [Required]
        [Display(Name = "Giá bậc 2 (31-60 m³)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [DataType(DataType.Currency)]
        public decimal Tier2Price { get; set; } = 11000;

        [Required]
        [Display(Name = "Giá bậc 3 (> 60 m³)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [DataType(DataType.Currency)]
        public decimal Tier3Price { get; set; } = 13200;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

