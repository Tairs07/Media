using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        [MinLength(3, ErrorMessage = "用户名长度至少3个字符")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [MaxLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [MinLength(6, ErrorMessage = "密码长度至少6位")]
        [MaxLength(100, ErrorMessage = "密码长度不能超过100个字符")]
        public string Password { get; set; } = string.Empty;
    }
}

