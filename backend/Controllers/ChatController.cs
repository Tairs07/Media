using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IQwenService _qwenService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ApplicationDbContext context,
            IQwenService qwenService,
            ILogger<ChatController> logger)
        {
            _context = context;
            _qwenService = qwenService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        /// <summary>
        /// 创建新的对话会话
        /// </summary>
        [HttpPost("sessions")]
        public async Task<ActionResult<ApiResponse<ChatSessionDto>>> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userId = GetUserId();

                var session = new ChatSession
                {
                    UserId = userId,
                    Title = string.IsNullOrWhiteSpace(request.Title) ? "新对话" : request.Title,
                    Model = request.Model,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();

                var dto = new ChatSessionDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    Model = session.Model,
                    MessageCount = 0,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt
                };

                return Ok(new ApiResponse<ChatSessionDto>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat session");
                return StatusCode(500, new ApiResponse<ChatSessionDto>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "创建会话失败" }
                });
            }
        }

        /// <summary>
        /// 获取会话列表
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult<ApiResponse<object>>> GetSessions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();

                var query = _context.ChatSessions
                    .Where(s => s.UserId == userId && !s.IsDeleted)
                    .OrderByDescending(s => s.UpdatedAt);

                var total = await query.CountAsync();
                var sessions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new ChatSessionDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Model = s.Model,
                        MessageCount = s.Messages.Count,
                        LastMessageAt = s.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        items = sessions,
                        total,
                        page,
                        pageSize,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat sessions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "获取会话列表失败" }
                });
            }
        }

        /// <summary>
        /// 获取会话详情（包含消息）
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<ApiResponse<ChatSessionDetailDto>>> GetSessionDetail(int sessionId)
        {
            try
            {
                var userId = GetUserId();

                var session = await _context.ChatSessions
                    .Include(s => s.Messages)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted);

                if (session == null)
                {
                    return NotFound(new ApiResponse<ChatSessionDetailDto>
                    {
                        Success = false,
                        Error = new ErrorInfo { Message = "会话不存在" }
                    });
                }

                var dto = new ChatSessionDetailDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    Model = session.Model,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt,
                    Messages = session.Messages
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new ChatMessageDto
                        {
                            Id = m.Id,
                            SessionId = m.SessionId,
                            Role = m.Role,
                            Content = m.Content,
                            Model = m.Model,
                            TokenCount = m.TokenCount,
                            CreatedAt = m.CreatedAt
                        })
                        .ToList()
                };

                return Ok(new ApiResponse<ChatSessionDetailDto>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session detail: {sessionId}");
                return StatusCode(500, new ApiResponse<ChatSessionDetailDto>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "获取会话详情失败" }
                });
            }
        }

        /// <summary>
        /// 发送消息（SSE流式响应）
        /// </summary>
        [HttpPost("sessions/{sessionId}/messages")]
        public async Task SendMessage(int sessionId, [FromBody] SendMessageRequest request)
        {
            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                var userId = GetUserId();

                // 验证会话存在且属于当前用户
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted, cancellationToken);

                if (session == null)
                {
                    await SendSseError("会话不存在");
                    return;
                }

                // 配置SSE响应
                Response.ContentType = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Connection"] = "keep-alive";
                Response.Headers["X-Accel-Buffering"] = "no";

                // 保存用户消息
                var userMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "user",
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(userMessage);
                await _context.SaveChangesAsync(cancellationToken);

                // 发送开始事件
                await SendSseEvent(new SseEventData
                {
                    Type = "start",
                    MessageId = userMessage.Id
                });

                // 获取历史消息（最近10条，避免token过多）
                var historyMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync(cancellationToken);

                // 流式调用Qwen API
                var fullContent = new StringBuilder();
                var model = request.Model ?? session.Model;

                await foreach (var delta in _qwenService.StreamChatAsync(model, historyMessages, cancellationToken))
                {
                    fullContent.Append(delta);
                    
                    await SendSseEvent(new SseEventData
                    {
                        Type = "content",
                        Delta = delta
                    });

                    await Response.Body.FlushAsync(cancellationToken);
                }

                // 保存AI响应
                var assistantMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = fullContent.ToString(),
                    Model = model,
                    TokenCount = fullContent.Length, // 简化处理，实际应该计算真实token数
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(assistantMessage);

                // 更新会话时间
                session.UpdatedAt = DateTime.UtcNow;
                
                // 自动生成标题（如果是第一条消息）
                if (session.Title == "新对话" && historyMessages.Count == 1)
                {
                    session.Title = request.Content.Length > 30 
                        ? request.Content.Substring(0, 30) + "..." 
                        : request.Content;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // 发送完成事件
                await SendSseEvent(new SseEventData
                {
                    Type = "done",
                    MessageId = assistantMessage.Id,
                    TokenCount = assistantMessage.TokenCount
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Stream cancelled for session {sessionId}");
                await SendSseEvent(new SseEventData
                {
                    Type = "cancelled"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message: {ex.Message}");
                await SendSseError(ex.Message);
            }
        }

        /// <summary>
        /// 更新会话标题
        /// </summary>
        [HttpPut("sessions/{sessionId}")]
        public async Task<ActionResult<ApiResponse<ChatSessionDto>>> UpdateSession(
            int sessionId,
            [FromBody] UpdateSessionRequest request)
        {
            try
            {
                var userId = GetUserId();

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted);

                if (session == null)
                {
                    return NotFound(new ApiResponse<ChatSessionDto>
                    {
                        Success = false,
                        Error = new ErrorInfo { Message = "会话不存在" }
                    });
                }

                session.Title = request.Title;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var dto = new ChatSessionDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    Model = session.Model,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt
                };

                return Ok(new ApiResponse<ChatSessionDto>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating session: {sessionId}");
                return StatusCode(500, new ApiResponse<ChatSessionDto>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "更新会话失败" }
                });
            }
        }

        /// <summary>
        /// 删除会话（软删除）
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSession(int sessionId)
        {
            try
            {
                var userId = GetUserId();

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorInfo { Message = "会话不存在" }
                    });
                }

                session.IsDeleted = true;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting session: {sessionId}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "删除会话失败" }
                });
            }
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        [HttpGet("models")]
        public ActionResult<ApiResponse<List<QwenModelInfo>>> GetModels()
        {
            try
            {
                var models = _qwenService.GetAvailableModels();
                return Ok(new ApiResponse<List<QwenModelInfo>>
                {
                    Success = true,
                    Data = models
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models");
                return StatusCode(500, new ApiResponse<List<QwenModelInfo>>
                {
                    Success = false,
                    Error = new ErrorInfo { Message = "获取模型列表失败" }
                });
            }
        }

        #region SSE Helper Methods

        private async Task SendSseEvent(SseEventData data)
        {
            var json = JsonSerializer.Serialize(data);
            await Response.WriteAsync($"data: {json}\n\n");
        }

        private async Task SendSseError(string errorMessage)
        {
            await SendSseEvent(new SseEventData
            {
                Type = "error",
                Error = errorMessage
            });
        }

        #endregion
    }
}

