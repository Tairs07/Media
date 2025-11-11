using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /// <summary>
    /// 对话消息实体
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属会话ID
        /// </summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>
        /// 角色：user 或 assistant
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 使用的模型（仅AI回复有值）
        /// </summary>
        [MaxLength(50)]
        public string? Model { get; set; }

        /// <summary>
        /// Token数量
        /// </summary>
        public int? TokenCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 导航属性
        [ForeignKey(nameof(SessionId))]
        public virtual ChatSession? Session { get; set; }
    }
}
