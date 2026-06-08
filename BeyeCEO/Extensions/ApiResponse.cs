namespace BeyeCEO.API.Extensions
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApiResponse(bool success, T? data, string? message = null)
        {
            Success = success;
            Data = data;
            Message = message;
        }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
