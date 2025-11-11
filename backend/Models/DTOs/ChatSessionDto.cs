namespace backend.Models.DTOs
{
    /// <summary>
    /// 对话会话DTO（列表用）
    /// </summary>
    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 对话会话详情DTO（包含消息列表）
    /// </summary>
    public class ChatSessionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    /// <summary>
    /// 创建会话请求
    /// </summary>
    public class CreateSessionRequest
    {
        public string? Title { get; set; }
        public string Model { get; set; } = "qwen-plus";
    }

    /// <summary>
    /// 更新会话请求
    /// </summary>
    public class UpdateSessionRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
