namespace backend.Models.DTOs
{
    public class MediaFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public long FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsPublic { get; set; }
        public DateTime UploadedAt { get; set; }
        public UserInfoDto? User { get; set; }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class UploadResponse
    {
        public List<MediaFileDto> Files { get; set; } = new();
    }
}




