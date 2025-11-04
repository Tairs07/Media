using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<UserProfileDto>.ErrorResponse(
                        "USER_NOT_FOUND",
                        "用户不存在"
                    ));
                }

                // 获取用户上传的文件数量
                var mediaCounts = await _context.MediaFiles
                    .Where(m => m.UserId == id)
                    .GroupBy(m => m.FileType)
                    .Select(g => new { FileType = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalUploads = await _context.MediaFiles.CountAsync(m => m.UserId == id);
                var imageCount = mediaCounts.FirstOrDefault(m => m.FileType == "image")?.Count ?? 0;
                var videoCount = mediaCounts.FirstOrDefault(m => m.FileType == "video")?.Count ?? 0;

                var userDto = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    CreatedAt = user.CreatedAt,
                    TotalUploads = totalUploads,
                    ImageCount = imageCount,
                    VideoCount = videoCount
                };

                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(userDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败");
                return StatusCode(500, ApiResponse<UserProfileDto>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "获取用户信息失败"
                ));
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
            int id,
            [FromBody] UpdateUserRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null || currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse(
                        "USER_NOT_FOUND",
                        "用户不存在"
                    ));
                }

                // 更新允许修改的字段
                if (request.AvatarUrl != null)
                {
                    user.AvatarUrl = request.AvatarUrl;
                }

                if (request.Bio != null)
                {
                    user.Bio = request.Bio;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto, "更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "更新用户信息失败"
                ));
            }
        }

        /// <summary>
        /// 获取用户上传的文件列表
        /// </summary>
        [HttpGet("{id}/media")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResponse<MediaFileDto>>>> GetUserMedia(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? fileType = null,
            [FromQuery] string orderBy = "uploadedAt",
            [FromQuery] string order = "desc")
        {
            try
            {
                // 检查用户是否存在
                var userExists = await _context.Users.AnyAsync(u => u.Id == id && u.IsActive);
                if (!userExists)
                {
                    return NotFound(ApiResponse<PagedResponse<MediaFileDto>>.ErrorResponse(
                        "USER_NOT_FOUND",
                        "用户不存在"
                    ));
                }

                var query = _context.MediaFiles
                    .Include(m => m.User)
                    .Where(m => m.UserId == id)
                    .AsQueryable();

                // 权限控制：未登录用户只能看到公开文件
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null || currentUserId != id)
                {
                    query = query.Where(m => m.IsPublic);
                }

                // 文件类型筛选
                if (!string.IsNullOrEmpty(fileType))
                {
                    query = query.Where(m => m.FileType == fileType);
                }

                // 排序
                query = orderBy.ToLower() switch
                {
                    "viewcount" => order.ToLower() == "asc"
                        ? query.OrderBy(m => m.ViewCount)
                        : query.OrderByDescending(m => m.ViewCount),
                    "likecount" => order.ToLower() == "asc"
                        ? query.OrderBy(m => m.LikeCount)
                        : query.OrderByDescending(m => m.LikeCount),
                    _ => order.ToLower() == "asc"
                        ? query.OrderBy(m => m.UploadedAt)
                        : query.OrderByDescending(m => m.UploadedAt)
                };

                var total = await query.CountAsync();
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new MediaFileDto
                    {
                        Id = m.Id,
                        FileName = m.FileName,
                        FileType = m.FileType,
                        MimeType = m.MimeType,
                        FilePath = m.FilePath,
                        ThumbnailPath = m.ThumbnailPath,
                        FileSize = m.FileSize,
                        Width = m.Width,
                        Height = m.Height,
                        Duration = m.Duration,
                        Description = m.Description,
                        Tags = m.Tags,
                        ViewCount = m.ViewCount,
                        LikeCount = m.LikeCount,
                        IsPublic = m.IsPublic,
                        UploadedAt = m.UploadedAt,
                        User = m.User != null ? new UserInfoDto
                        {
                            Id = m.User.Id,
                            Username = m.User.Username,
                            AvatarUrl = m.User.AvatarUrl
                        } : null
                    })
                    .ToListAsync();

                var pagedResponse = new PagedResponse<MediaFileDto>
                {
                    Items = items,
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                };

                return Ok(ApiResponse<PagedResponse<MediaFileDto>>.SuccessResponse(pagedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户文件列表失败");
                return StatusCode(500, ApiResponse<PagedResponse<MediaFileDto>>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "获取用户文件列表失败"
                ));
            }
        }
    }

    public class UpdateUserRequest
    {
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalUploads { get; set; }
        public int ImageCount { get; set; }
        public int VideoCount { get; set; }
    }
}




