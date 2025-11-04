using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFileStorageService fileStorageService,
            ILogger<FilesController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// 获取文件流
        /// </summary>
        [HttpGet("{*filePath}")]
        public async Task<IActionResult> GetFile(string filePath)
        {
            try
            {
                // URL解码文件路径
                filePath = Uri.UnescapeDataString(filePath);

                var stream = await _fileStorageService.GetFileAsync(filePath);
                if (stream == null)
                {
                    return NotFound();
                }

                // 根据文件扩展名确定Content-Type
                var contentType = GetContentType(filePath);

                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件失败: {FilePath}", filePath);
                return StatusCode(500);
            }
        }
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }
    }
}




