namespace backend.Models.DTOs
{
    public class SessionListResponse
    {
        public List<ChatSessionDto> Items { get; set; } = new List<ChatSessionDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

