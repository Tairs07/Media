namespace backend.Models.DTOs
{
    /// <summary>
    /// 对话消息DTO
    /// </summary>
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? TokenCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 发送消息请求
    /// </summary>
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Model { get; set; }
    }

    /// <summary>
    /// SSE事件数据
    /// </summary>
    public class SseEventData
    {
        public string Type { get; set; } = string.Empty;
        public int? MessageId { get; set; }
        public string? Delta { get; set; }
        public int? TokenCount { get; set; }
        public string? Error { get; set; }
    }
}
