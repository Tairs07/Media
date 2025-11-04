using backend.Models;

namespace backend.Services
{
    public interface IJwtService
    {
        /// <summary>
        /// 生成JWT Token
        /// </summary>
        string GenerateToken(User user);

        /// <summary>
        /// 验证Token并获取用户ID
        /// </summary>
        int? ValidateToken(string token);
    }
}




