using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models; // Теперь этот using дает доступ к ApiResponses.cs и LoginResponseData
using System.Text.Json;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Claims; // Added for JWT
using Microsoft.IdentityModel.Tokens; // Added for SymmetricSecurityKey
using System.IdentityModel.Tokens.Jwt; // Added for JwtSecurityTokenHandler

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
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    Debug.WriteLine($"[DEBUG SERVER] LoginRequest after manual deserialization: Username='{request?.Username}', Password='{request?.Password}'");
                    return BadRequest(new ApiResponse // Теперь используем ApiResponse для ошибок
                    {
                        IsSuccess = false, // Заметьте, IsSuccess (PascalCase)
                        Message = "Неверный запрос: имя пользователя или пароль пусты."
                    });
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                if (user == null)
                {
                    return Unauthorized(new ApiResponse // Используем ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Пользователь не найден."
                    });
                }

                if (user.Password != request.Password)
                {
                    return Unauthorized(new ApiResponse // Используем ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Неверный пароль."
                    });
                }

                // ИСПРАВЛЕНИЕ: Генерируем JWT токен
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                // Используем тот же ключ, что и в Program.cs (для простоты)
                // В продакшене лучше получать из конфигурации
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key_for_jwt_development_purposes_only"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: null, // No issuer for simplicity in development
                    audience: null, // No audience for simplicity in development
                    claims: claims,
                    expires: DateTime.Now.AddDays(7), // Token valid for 7 days
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // !!! КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: ВОЗВРАЩАЕМ ApiResponse<AuthSuccessResponse> с токеном!!!
                return Ok(new ApiResponse<AuthSuccessResponse>
                {
                    IsSuccess = true,
                    Data = new AuthSuccessResponse
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Role = user.Role,
                        Token = tokenString // Возвращаем JWT токен
                    },
                    Message = "Вход успешно выполнен."
                });
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[DEBUG SERVER] JSON Deserialization Error: {ex.Message}");
                return BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    Message = $"Некорректный формат данных в запросе: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AuthController.Login: {ex.Message} - {ex.InnerException?.Message}");
                return StatusCode(500, new ApiResponse
                {
                    IsSuccess = false,
                    Message = $"Внутренняя ошибка сервера: {ex.Message}. Пожалуйста, попробуйте позже."
                });
            }
        }
    }

    // Класс LoginResponse Data теперь должен быть определен в VepsPlusApi/Models/LoginResponseData.cs
    // и использоваться здесь через using VepsPlusApi.Models;
}

