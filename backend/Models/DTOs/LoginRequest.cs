using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [MinLength(6, ErrorMessage = "密码长度至少6位")]
        public string Password { get; set; } = string.Empty;
    }
}




