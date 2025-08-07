using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using System.Text.Json; // Добавляем для JsonSerializer
using System.IO;
using System.Text;
using System.Diagnostics;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AuthController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login() // Теперь метод не принимает [FromBody] LoginRequest напрямую
        {
            string requestBody = "";
            try
            {
                // Временно читаем тело запроса вручную
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }
                Debug.WriteLine($"[DEBUG] AuthController received RAW Request Body for manual parsing: {requestBody}");

                // ВРУЧНУЮ десериализуем JSON
                // Используем JsonSerializer.Deserialize<LoginRequest> с теми же опциями, что и в Program.cs
                var request = JsonSerializer.Deserialize<LoginRequest>(requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Убедитесь, что эта опция есть!

                // Теперь проверяем полученный объект
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    Debug.WriteLine($"[DEBUG] LoginRequest after manual deserialization: Username='{request?.Username}', Password='{request?.Password}'");
                    return BadRequest(new LoginResponse
                    {
                        isSuccess = false,
                        message = "Неверный запрос: имя пользователя или пароль пусты."
                    });
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                if (user == null)
                {
                    return Unauthorized(new LoginResponse
                    {
                        isSuccess = false,
                        message = "Пользователь не найден."
                    });
                }

                if (user.Password != request.Password)
                {
                    return Unauthorized(new LoginResponse
                    {
                        isSuccess = false,
                        message = "Неверный пароль."
                    });
                }

                return Ok(new LoginResponse
                {
                    isSuccess = true,
                    userId = user.Id,
                    username = user.Username,
                    role = user.Role,
                    message = "Вход успешно выполнен."
                });
            }
            catch (JsonException ex) // Ошибка при десериализации JSON
            {
                Debug.WriteLine($"[DEBUG] JSON Deserialization Error: {ex.Message} - Raw Body: {requestBody}");
                return BadRequest(new LoginResponse
                {
                    isSuccess = false,
                    message = $"Некорректный формат данных в запросе: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AuthController.Login: {ex.Message} - {ex.InnerException?.Message}");
                return StatusCode(500, new LoginResponse
                {
                    isSuccess = false,
                    message = $"Внутренняя ошибка сервера: {ex.Message}. Пожалуйста, попробуйте позже."
                });
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool isSuccess { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public string role { get; set; }
        public string message { get; set; }
    }
}

