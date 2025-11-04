namespace backend.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// 保存文件
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, int userId, string fileType);

        /// <summary>
        /// 保存缩略图
        /// </summary>
        Task<string?> SaveThumbnailAsync(string sourcePath, int userId);

        /// <summary>
        /// 删除文件
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// 获取文件流
        /// </summary>
        Task<Stream?> GetFileAsync(string filePath);

        /// <summary>
        /// 获取文件URL
        /// </summary>
        string GetFileUrl(string filePath);

        /// <summary>
        /// 检查文件类型是否允许
        /// </summary>
        bool IsAllowedFileType(string mimeType, string fileType);

        /// <summary>
        /// 检查文件大小是否允许
        /// </summary>
        bool IsAllowedFileSize(long fileSize, string fileType);
    }
}




