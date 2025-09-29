using System.Text.Json.Serialization; // Для [JsonPropertyName]

namespace VEPS_Plus.ViewModels // Или VEPS_Plus.Models.Shared, если новая папка
{
    // Универсальная модель ответа для API с данными
    public class ApiResponse<T>
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    // Универсальная модель ответа для API без данных
    public class ApiResponse
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    // Model for successful authentication response including JWT token
    public class AuthSuccessResponse
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    // Модель пользователя для аутентификации
    public class User
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    // Модель профиля пользователя
    public class Profile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    // Модель для запроса аутентификации
    public class LoginRequest
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }

    // Модель для обновления профиля
    public class ProfileUpdateRequest
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }

    // Модель для смены пароля
    public class ChangePasswordRequest
    {
        [JsonPropertyName("currentPassword")]
        public string? CurrentPassword { get; set; }

        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }
    }

    public class PushRegistrationRequest
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("registrationToken")]
        public string? RegistrationToken { get; set; }
    }
}