using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace backend.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileStorageService> _logger;

        private readonly string _basePath;
        private readonly long _maxImageSize;
        private readonly long _maxVideoSize;
        private readonly List<string> _allowedImageTypes;
        private readonly List<string> _allowedVideoTypes;
        private readonly int _thumbnailWidth;
        private readonly int _thumbnailHeight;

        public LocalFileStorageService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<LocalFileStorageService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            var fileStorageConfig = _configuration.GetSection("FileStorage");
            _basePath = fileStorageConfig["LocalPath"] ?? "wwwroot/uploads";
            _maxImageSize = long.Parse(fileStorageConfig["MaxImageSize"] ?? "10485760");
            _maxVideoSize = long.Parse(fileStorageConfig["MaxVideoSize"] ?? "104857600");
            _allowedImageTypes = fileStorageConfig.GetSection("AllowedImageTypes").Get<List<string>>() ?? new();
            _allowedVideoTypes = fileStorageConfig.GetSection("AllowedVideoTypes").Get<List<string>>() ?? new();
            _thumbnailWidth = int.Parse(fileStorageConfig["ThumbnailWidth"] ?? "300");
            _thumbnailHeight = int.Parse(fileStorageConfig["ThumbnailHeight"] ?? "300");

            // 确保目录存在
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            var paths = new[]
            {
                Path.Combine(_basePath, "images"),
                Path.Combine(_basePath, "videos"),
                Path.Combine(_basePath, "thumbnails")
            };

            foreach (var path in paths)
            {
                var fullPath = Path.Combine(_environment.ContentRootPath, path);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    SetLinuxPermissions(fullPath, "755");
                    _logger.LogInformation("创建并设置目录权限: {Path}", fullPath);
                }
            }
        }

        private string GenerateFileName(int userId, string extension)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8))
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 8);
            return $"{userId}_{timestamp}_{randomString}{extension}";
        }

        private void SetLinuxPermissions(string path, string permissions)
        {
            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"{permissions} \"{path}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "设置权限失败: {Path}", path);
                }
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, int userId, string fileType)
        {
            if (!IsAllowedFileType(file.ContentType, fileType))
            {
                throw new ArgumentException($"不允许的文件类型: {file.ContentType}");
            }

            if (!IsAllowedFileSize(file.Length, fileType))
            {
                throw new ArgumentException($"文件大小超过限制");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = GenerateFileName(userId, extension);
            
            var subfolder = fileType == "image" ? "images" : "videos";
            var year = DateTime.UtcNow.Year.ToString();
            var month = DateTime.UtcNow.Month.ToString("D2");
            var day = DateTime.UtcNow.Day.ToString("D2");

            var relativePath = Path.Combine(subfolder, year, month, day, fileName);
            var fullPath = Path.Combine(_environment.ContentRootPath, _basePath, relativePath);
            var directory = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                SetLinuxPermissions(directory, "755");
                _logger.LogInformation("创建并设置目录权限: {Directory}", directory);
            }

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            // 设置文件权限
            SetLinuxPermissions(fullPath, "644");

            return relativePath.Replace('\\', '/');
        }

        public async Task<string?> SaveThumbnailAsync(string sourcePath, int userId)
        {
            try
            {
                var fullSourcePath = Path.Combine(_environment.ContentRootPath, _basePath, sourcePath);
                
                if (!File.Exists(fullSourcePath))
                {
                    return null;
                }

                var extension = Path.GetExtension(sourcePath);
                var fileName = Path.GetFileNameWithoutExtension(sourcePath);
                var thumbnailFileName = $"{fileName}_thumb{extension}";
                
                var relativeThumbnailPath = Path.Combine("thumbnails", DateTime.UtcNow.Year.ToString(), 
                    DateTime.UtcNow.Month.ToString("D2"), DateTime.UtcNow.Day.ToString("D2"), thumbnailFileName);
                
                var fullThumbnailPath = Path.Combine(_environment.ContentRootPath, _basePath, relativeThumbnailPath);
                var thumbnailDirectory = Path.GetDirectoryName(fullThumbnailPath)!;

                if (!Directory.Exists(thumbnailDirectory))
                {
                    Directory.CreateDirectory(thumbnailDirectory);
                    SetLinuxPermissions(thumbnailDirectory, "755");
                    _logger.LogInformation("创建并设置缩略图目录权限: {Directory}", thumbnailDirectory);
                }

                using (var image = await Image.LoadAsync(fullSourcePath))
                {
                    image.Mutate(x => x
                        .Resize(new ResizeOptions
                        {
                            Size = new Size(_thumbnailWidth, _thumbnailHeight),
                            Mode = ResizeMode.Max
                        }));

                    await image.SaveAsync(fullThumbnailPath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    {
                        Quality = 80
                    });
                }

                // 设置缩略图权限
                SetLinuxPermissions(fullThumbnailPath, "644");

                return relativeThumbnailPath.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成缩略图失败: {SourcePath}", sourcePath);
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.ContentRootPath, _basePath, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件失败: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<Stream?> GetFileAsync(string filePath)
        {
            try
            {
                // 确保路径分隔符为正确的格式
                filePath = filePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

                var fullPath = Path.Combine(_environment.ContentRootPath, _basePath, filePath);

                _logger.LogInformation("尝试获取文件: {FilePath}, 完整路径: {FullPath}, 文件存在: {Exists}",
                    filePath, fullPath, File.Exists(fullPath));

                if (File.Exists(fullPath))
                {
                    return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

                // 如果文件不存在，记录目录内容以便调试
                var directory = Path.GetDirectoryName(fullPath);
                if (directory != null && Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory);
                    _logger.LogWarning("文件不存在，目录内容: {Directory}, 文件列表: {Files}",
                        directory, string.Join(", ", files.Select(Path.GetFileName)));
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件失败: {FilePath}", filePath);
                return null;
            }
        }

        public string GetFileUrl(string filePath)
        {
            return $"/api/files/{Uri.EscapeDataString(filePath)}";
        }

        public bool IsAllowedFileType(string mimeType, string fileType)
        {
            if (fileType == "image")
            {
                return _allowedImageTypes.Contains(mimeType);
            }
            else if (fileType == "video")
            {
                return _allowedVideoTypes.Contains(mimeType);
            }
            return false;
        }

        public bool IsAllowedFileSize(long fileSize, string fileType)
        {
            if (fileType == "image")
            {
                return fileSize <= _maxImageSize;
            }
            else if (fileType == "video")
            {
                return fileSize <= _maxVideoSize;
            }
            return false;
        }
    }
}

