namespace VepsPlusApi.Models // Или VepsPlusApi.Shared, если новая папка
{
    // Универсальная модель ответа для API с данными
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }

    // Универсальная модель ответа для API без данных
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}