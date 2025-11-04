namespace backend.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public ErrorInfo? Error { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "操作成功"
            };
        }

        public static ApiResponse<T> ErrorResponse(string errorCode, string errorMessage)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = new ErrorInfo
                {
                    Code = errorCode,
                    Message = errorMessage
                }
            };
        }
    }

    public class ErrorInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}




