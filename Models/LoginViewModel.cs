using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class WaterInquiryViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã khách hàng")]
        [Display(Name = "Mã khách hàng")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn quý")]
        [Display(Name = "Quý")]
        public int Quarter { get; set; } = (DateTime.Now.Month - 1) / 3 + 1;

        [Required(ErrorMessage = "Vui lòng chọn năm")]
        [Display(Name = "Năm")]
        public int Year { get; set; } = DateTime.Now.Year;
    }
}
