using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /// <summary>
    /// 对话会话实体
    /// </summary>
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属用户ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 会话标题
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "新对话";

        /// <summary>
        /// 使用的模型
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = "qwen-plus";

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否已删除（软删除）
        /// </summary>
        [Required]
        public bool IsDeleted { get; set; } = false;

        // 导航属性
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
