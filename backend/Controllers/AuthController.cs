using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // 验证用户名是否已存在
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(
                        "USERNAME_EXISTS",
                        "用户名已存在"
                    ));
                }

                // 验证邮箱是否已存在
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(
                        "EMAIL_EXISTS",
                        "邮箱已被注册"
                    ));
                }

                // 加密密码
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 创建用户
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 生成Token
                var token = _jwtService.GenerateToken(user);

                // 创建响应
                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        AvatarUrl = user.AvatarUrl,
                        Bio = user.Bio,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "注册成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册失败");
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "注册失败，请稍后重试"
                ));
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // 查找用户（支持用户名或邮箱登录）
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

                if (user == null)
                {
                    return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(
                        "INVALID_CREDENTIALS",
                        "用户名或密码错误"
                    ));
                }

                // 验证密码
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(
                        "INVALID_CREDENTIALS",
                        "用户名或密码错误"
                    ));
                }

                // 检查用户是否激活
                if (!user.IsActive)
                {
                    return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(
                        "USER_INACTIVE",
                        "账户已被禁用"
                    ));
                }

                // 生成Token
                var token = _jwtService.GenerateToken(user);

                // 保存会话（可选）
                // TODO: 实现会话保存逻辑

                // 创建响应
                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        AvatarUrl = user.AvatarUrl,
                        Bio = user.Bio,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "登录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录失败");
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "登录失败，请稍后重试"
                ));
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse(
                        "UNAUTHORIZED",
                        "未授权"
                    ));
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse(
                        "USER_NOT_FOUND",
                        "用户不存在"
                    ));
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "获取用户信息失败"
                ));
            }
        }

        /// <summary>
        /// 刷新Token（可选功能）
        /// </summary>
        [Authorize]
        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(
                        "UNAUTHORIZED",
                        "未授权"
                    ));
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<AuthResponse>.ErrorResponse(
                        "USER_NOT_FOUND",
                        "用户不存在"
                    ));
                }

                // 生成新Token
                var token = _jwtService.GenerateToken(user);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        AvatarUrl = user.AvatarUrl,
                        Bio = user.Bio,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Token刷新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token失败");
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "刷新Token失败"
                ));
            }
        }
    }
}

