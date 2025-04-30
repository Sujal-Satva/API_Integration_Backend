namespace task_14.Models
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }

        public ApiResponse(T? data = default, string? message = null, string? error = null)
        {
            Data = data;
            Message = message;
            Error = error;
        }
    }

    public class TokenResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RealmId { get; set; }
    }


}
