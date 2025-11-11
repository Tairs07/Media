using backend.Models;

namespace backend.Services
{
    /// <summary>
    /// 通义千问API服务接口
    /// </summary>
    public interface IQwenService
    {
        /// <summary>
        /// 流式调用通义千问API
        /// </summary>
        /// <param name="model">模型名称</param>
        /// <param name="messages">对话历史</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>流式返回的文本增量</returns>
        IAsyncEnumerable<string> StreamChatAsync(
            string model,
            List<ChatMessage> messages,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// 获取可用的模型列表
        /// </summary>
        List<QwenModelInfo> GetAvailableModels();
    }

    /// <summary>
    /// 通义千问模型信息
    /// </summary>
    public class QwenModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
    }
}



