using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class MediaFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty; // "image" or "video"

        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty; // "image/jpeg", "video/mp4" 等

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }

        public long FileSize { get; set; } // 文件大小（字节）

        public int? Width { get; set; } // 图片/视频宽度（像素）

        public int? Height { get; set; } // 图片/视频高度（像素）

        public int? Duration { get; set; } // 视频时长（秒）

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Tags { get; set; } // 标签，用逗号分隔

        public int ViewCount { get; set; } = 0; // 浏览次数

        public int LikeCount { get; set; } = 0; // 点赞数

        public bool IsPublic { get; set; } = true; // 是否公开

        // 外键
        [Required]
        public int UserId { get; set; }

        // 导航属性
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
