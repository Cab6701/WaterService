using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterService.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Mã khách hàng")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên khách hàng")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Phone number must be 10-11 digits")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual List<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

        [NotMapped]
        public static List<SelectListItem> AddressOptions => SelectListHelper.GetAddressSelectList();
    }

    public enum CustomerAddress
    {
        [Display(Name = "Tân Tiến")]
        TanTien,
        [Display(Name = "Chi Lê")]
        ChiLe,
        [Display(Name = "Bắc Sơn")]
        BacSon,
        [Display(Name = "Minh Sơn")]
        MinhSon,
        [Display(Name = "Hồng Hoàng")]
        HongHoang,
        [Display(Name = "Minh Tân")]
        MinhTan,
        [Display(Name = "Hồng Quang")]
        HongQuang,
        [Display(Name = "Quang Trung")]
        QuangTrung,
        [Display(Name = "Minh Khai")]
        MinhKhai,
        [Display(Name = "Cơ Quan Doanh Nghiệp")]
        Organization
    }
}