using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using backend.Models;

namespace backend.Services
{
    /// <summary>
    /// 通义千问API服务实现
    /// </summary>
    public class QwenService : IQwenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QwenService> _logger;
        private readonly string _apiKey;
        private readonly string _endpoint;

        public QwenService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<QwenService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _apiKey = _configuration["Qwen:ApiKey"] ?? throw new InvalidOperationException("Qwen:ApiKey not configured");
            _endpoint = _configuration["Qwen:Endpoint"] ?? "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";

            // 配置HttpClient
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-DashScope-SSE", "enable");
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("Qwen:Timeout", 60000) / 1000);
        }

        public async IAsyncEnumerable<string> StreamChatAsync(
            string model,
            List<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 构建请求体
            var requestBody = new
            {
                model = model,
                input = new
                {
                    messages = messages.Select(m => new
                    {
                        role = m.Role,
                        content = m.Content
                    }).ToList()
                },
                parameters = new
                {
                    result_format = "message",
                    incremental_output = true,
                    temperature = _configuration.GetValue<double>("Qwen:Temperature", 0.7),
                    top_p = _configuration.GetValue<double>("Qwen:TopP", 0.8)
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation($"Calling Qwen API with model: {model}");

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // 发送请求并接收SSE流
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullContent = new StringBuilder();

            // 读取SSE流
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // SSE格式：data: {...}
                if (line.StartsWith("data:"))
                {
                    var json = line.Substring(5).Trim();
                    
                    // 跳过结束标记
                    if (json == "[DONE]")
                        break;

                    // 解析JSON（捕获错误但不在try中yield）
                    JsonDocument? doc = null;
                    try
                    {
                        doc = JsonDocument.Parse(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning($"Failed to parse SSE data: {json}, Error: {ex.Message}");
                        continue;
                    }

                    using (doc)
                    {
                        var root = doc.RootElement;

                        // 检查是否有错误
                        if (root.TryGetProperty("code", out var codeElement))
                        {
                            var errorCode = codeElement.GetString();
                            var errorMessage = root.TryGetProperty("message", out var msgElement) 
                                ? msgElement.GetString() 
                                : "Unknown error";
                            _logger.LogError($"Qwen API error: {errorCode} - {errorMessage}");
                            throw new Exception($"Qwen API error: {errorCode} - {errorMessage}");
                        }

                        // 提取增量内容
                        if (root.TryGetProperty("output", out var output))
                        {
                            if (output.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                            {
                                var choice = choices[0];
                                if (choice.TryGetProperty("message", out var message))
                                {
                                    if (message.TryGetProperty("content", out var content))
                                    {
                                        var delta = content.GetString();
                                        if (!string.IsNullOrEmpty(delta))
                                        {
                                            // 通义千问API的incremental_output模式下，content本身就是增量内容
                                            fullContent.Append(delta);
                                            yield return delta;
                                        }
                                    }
                                }
                            }

                            // 检查是否结束
                            if (output.TryGetProperty("finish_reason", out var finishReason))
                            {
                                var reason = finishReason.GetString();
                                if (reason == "stop" || reason == "length")
                                {
                                    _logger.LogInformation($"Qwen API stream finished: {reason}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation($"Qwen API stream completed. Total content length: {fullContent.Length}");
        }

        public List<QwenModelInfo> GetAvailableModels()
        {
            return new List<QwenModelInfo>
            {
                new QwenModelInfo
                {
                    Name = "qwen-turbo",
                    DisplayName = "通义千问-Turbo",
                    MaxTokens = 8000
                },
                new QwenModelInfo
                {
                    Name = "qwen-plus",
                    DisplayName = "通义千问-Plus",
                    MaxTokens = 32000
                },
                new QwenModelInfo
                {
                    Name = "qwen-max",
                    DisplayName = "通义千问-Max",
                    MaxTokens = 8000
                }
            };
        }
    }
}

