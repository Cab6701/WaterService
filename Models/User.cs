using System.ComponentModel.DataAnnotations;

namespace WaterService.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên hiển thị")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }

    public enum UserRole
    {
        [Display(Name = "Quản trị viên")]
        Admin,
        [Display(Name = "Người dùng")]
        User
    }
}
