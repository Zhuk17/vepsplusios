namespace VepsPlusApi.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public T Data { get; set; }
        public string Message { get; set; }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}