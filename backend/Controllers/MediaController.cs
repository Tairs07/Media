using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using SixLabors.ImageSharp;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<MediaController> _logger;
        private readonly IWebHostEnvironment _environment;

        public MediaController(
            ApplicationDbContext context,
            IFileStorageService fileStorageService,
            ILogger<MediaController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _environment = environment;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<UploadResponse>>> UploadFiles(
            [FromForm] List<IFormFile> files,
            [FromForm] string title,
            [FromForm] string? description = null,
            [FromForm] string? tags = null,
            [FromForm] bool isPublic = true)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest(ApiResponse<UploadResponse>.ErrorResponse(
                        "NO_FILES",
                        "请选择要上传的文件"
                    ));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(ApiResponse<UploadResponse>.ErrorResponse(
                        "TITLE_REQUIRED",
                        "请输入标题"
                    ));
                }

                var userId = GetCurrentUserId();
                var uploadedFiles = new List<MediaFileDto>();

                foreach (var file in files)
                {
                    // 确定文件类型
                    var fileType = DetermineFileType(file.ContentType);
                    if (string.IsNullOrEmpty(fileType))
                    {
                        continue; // 跳过不支持的文件类型
                    }

                    // 验证文件类型和大小
                    if (!_fileStorageService.IsAllowedFileType(file.ContentType, fileType))
                    {
                        _logger.LogWarning("不允许的文件类型: {ContentType}", file.ContentType);
                        continue;
                    }

                    if (!_fileStorageService.IsAllowedFileSize(file.Length, fileType))
                    {
                        _logger.LogWarning("文件大小超过限制: {FileName}, {FileSize}", file.FileName, file.Length);
                        continue;
                    }

                    // 保存文件
                    var filePath = await _fileStorageService.SaveFileAsync(file, userId, fileType);

                    // 获取文件元数据
                    int? width = null;
                    int? height = null;
                    string? thumbnailPath = null;

                    if (fileType == "image")
                    {
                        try
                        {
                            // 获取图片尺寸
                            var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads", filePath);
                            using (var image = await Image.LoadAsync(fullPath))
                            {
                                width = image.Width;
                                height = image.Height;
                            }

                            // 生成缩略图
                            thumbnailPath = await _fileStorageService.SaveThumbnailAsync(filePath, userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "处理图片失败: {FilePath}", filePath);
                        }
                    }

                    // 保存到数据库
                    var mediaFile = new MediaFile
                    {
                        Title = title,
                        FileName = file.FileName,
                        FileType = fileType,
                        MimeType = file.ContentType,
                        FilePath = filePath,
                        ThumbnailPath = thumbnailPath,
                        FileSize = file.Length,
                        Width = width,
                        Height = height,
                        Description = description,
                        Tags = tags,
                        IsPublic = isPublic,
                        UserId = userId,
                        UploadedAt = DateTime.UtcNow,
                        ViewCount = 0,
                        LikeCount = 0
                    };

                    _context.MediaFiles.Add(mediaFile);
                    await _context.SaveChangesAsync();

                    // 创建DTO
                    var fileDto = new MediaFileDto
                    {
                        Id = mediaFile.Id,
                        Title = mediaFile.Title,
                        FileName = mediaFile.FileName,
                        FileType = mediaFile.FileType,
                        MimeType = mediaFile.MimeType,
                        FilePath = mediaFile.FilePath,
                        ThumbnailPath = mediaFile.ThumbnailPath,
                        FileSize = mediaFile.FileSize,
                        Width = mediaFile.Width,
                        Height = mediaFile.Height,
                        Duration = mediaFile.Duration,
                        Description = mediaFile.Description,
                        Tags = mediaFile.Tags,
                        ViewCount = mediaFile.ViewCount,
                        LikeCount = mediaFile.LikeCount,
                        IsPublic = mediaFile.IsPublic,
                        UploadedAt = mediaFile.UploadedAt
                    };

                    uploadedFiles.Add(fileDto);
                }

                if (uploadedFiles.Count == 0)
                {
                    return BadRequest(ApiResponse<UploadResponse>.ErrorResponse(
                        "UPLOAD_FAILED",
                        "没有文件成功上传"
                    ));
                }

                var response = new UploadResponse
                {
                    Files = uploadedFiles
                };

                return Ok(ApiResponse<UploadResponse>.SuccessResponse(response, "上传成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传文件失败");
                return StatusCode(500, ApiResponse<UploadResponse>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "上传失败，请稍后重试"
                ));
            }
        }

        /// <summary>
        /// 获取文件列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResponse<MediaFileDto>>>> GetFiles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? fileType = null,
            [FromQuery] int? userId = null,
            [FromQuery] string orderBy = "uploadedAt",
            [FromQuery] string order = "desc")
        {
            try
            {
                var query = _context.MediaFiles
                    .Include(m => m.User)
                    .AsQueryable();

                // 筛选：只显示公开文件（除非是当前用户的文件）
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    // 未登录用户只能看到公开文件
                    query = query.Where(m => m.IsPublic);
                }
                else
                {
                    // 登录用户可以看到公开文件或自己的文件
                    query = query.Where(m => m.IsPublic || m.UserId == currentUserId);
                }

                // 文件类型筛选
                if (!string.IsNullOrEmpty(fileType))
                {
                    query = query.Where(m => m.FileType == fileType);
                }

                // 用户筛选
                if (userId.HasValue)
                {
                    query = query.Where(m => m.UserId == userId.Value);
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
                        Title = m.Title,
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
                _logger.LogError(ex, "获取文件列表失败");
                return StatusCode(500, ApiResponse<PagedResponse<MediaFileDto>>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "获取文件列表失败"
                ));
            }
        }

        /// <summary>
        /// 获取文件详情
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<MediaFileDto>>> GetFile(int id)
        {
            try
            {
                var mediaFile = await _context.MediaFiles
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mediaFile == null)
                {
                    return NotFound(ApiResponse<MediaFileDto>.ErrorResponse(
                        "FILE_NOT_FOUND",
                        "文件不存在"
                    ));
                }

                // 检查权限
                var currentUserId = GetCurrentUserId();
                if (!mediaFile.IsPublic && mediaFile.UserId != currentUserId)
                {
                    return Forbid();
                }

                // 增加浏览次数
                mediaFile.ViewCount++;
                await _context.SaveChangesAsync();

                var fileDto = new MediaFileDto
                {
                    Id = mediaFile.Id,
                    Title = mediaFile.Title,
                    FileName = mediaFile.FileName,
                    FileType = mediaFile.FileType,
                    MimeType = mediaFile.MimeType,
                    FilePath = mediaFile.FilePath,
                    ThumbnailPath = mediaFile.ThumbnailPath,
                    FileSize = mediaFile.FileSize,
                    Width = mediaFile.Width,
                    Height = mediaFile.Height,
                    Duration = mediaFile.Duration,
                    Description = mediaFile.Description,
                    Tags = mediaFile.Tags,
                    ViewCount = mediaFile.ViewCount,
                    LikeCount = mediaFile.LikeCount,
                    IsPublic = mediaFile.IsPublic,
                    UploadedAt = mediaFile.UploadedAt,
                    User = mediaFile.User != null ? new UserInfoDto
                    {
                        Id = mediaFile.User.Id,
                        Username = mediaFile.User.Username,
                        AvatarUrl = mediaFile.User.AvatarUrl
                    } : null
                };

                return Ok(ApiResponse<MediaFileDto>.SuccessResponse(fileDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件详情失败");
                return StatusCode(500, ApiResponse<MediaFileDto>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "获取文件详情失败"
                ));
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFile(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var mediaFile = await _context.MediaFiles.FindAsync(id);

                if (mediaFile == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        "FILE_NOT_FOUND",
                        "文件不存在"
                    ));
                }

                // 检查权限：只有文件所有者可以删除
                if (mediaFile.UserId != userId)
                {
                    return Forbid();
                }

                // 删除物理文件
                await _fileStorageService.DeleteFileAsync(mediaFile.FilePath);
                if (!string.IsNullOrEmpty(mediaFile.ThumbnailPath))
                {
                    await _fileStorageService.DeleteFileAsync(mediaFile.ThumbnailPath);
                }

                // 从数据库删除
                _context.MediaFiles.Remove(mediaFile);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new { }, "删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件失败");
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "删除文件失败"
                ));
            }
        }

        /// <summary>
        /// 更新文件信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<MediaFileDto>>> UpdateFile(
            int id,
            [FromBody] UpdateMediaFileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var mediaFile = await _context.MediaFiles.FindAsync(id);

                if (mediaFile == null)
                {
                    return NotFound(ApiResponse<MediaFileDto>.ErrorResponse(
                        "FILE_NOT_FOUND",
                        "文件不存在"
                    ));
                }

                // 检查权限
                if (mediaFile.UserId != userId)
                {
                    return Forbid();
                }

                // 更新信息
                if (request.Description != null)
                {
                    mediaFile.Description = request.Description;
                }

                if (request.Tags != null)
                {
                    mediaFile.Tags = request.Tags;
                }

                if (request.IsPublic.HasValue)
                {
                    mediaFile.IsPublic = request.IsPublic.Value;
                }

                mediaFile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 返回更新后的数据
                var fileDto = new MediaFileDto
                {
                    Id = mediaFile.Id,
                    Title = mediaFile.Title,
                    FileName = mediaFile.FileName,
                    FileType = mediaFile.FileType,
                    MimeType = mediaFile.MimeType,
                    FilePath = mediaFile.FilePath,
                    ThumbnailPath = mediaFile.ThumbnailPath,
                    FileSize = mediaFile.FileSize,
                    Width = mediaFile.Width,
                    Height = mediaFile.Height,
                    Duration = mediaFile.Duration,
                    Description = mediaFile.Description,
                    Tags = mediaFile.Tags,
                    ViewCount = mediaFile.ViewCount,
                    LikeCount = mediaFile.LikeCount,
                    IsPublic = mediaFile.IsPublic,
                    UploadedAt = mediaFile.UploadedAt
                };

                return Ok(ApiResponse<MediaFileDto>.SuccessResponse(fileDto, "更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新文件失败");
                return StatusCode(500, ApiResponse<MediaFileDto>.ErrorResponse(
                    "INTERNAL_ERROR",
                    "更新文件失败"
                ));
            }
        }

        private string DetermineFileType(string contentType)
        {
            if (contentType.StartsWith("image/"))
            {
                return "image";
            }
            else if (contentType.StartsWith("video/"))
            {
                return "video";
            }
            return string.Empty;
        }
    }

    public class UpdateMediaFileRequest
    {
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

