using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    Debug.WriteLine($"[DEBUG SERVER] LoginRequest after manual deserialization: Username='{request?.Username}', Password='{request?.Password}'");
                    return BadRequest(new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Неверный запрос: имя пользователя или пароль пусты."
                    });
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                long dbQueryTime = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine($"[AuthController] DB Query (FirstOrDefaultAsync) Duration: {dbQueryTime} ms");

                if (user == null)
                {
                    return Unauthorized(new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Пользователь не найден."
                    });
                }

                if (user.Password != request.Password)
                {
                    return Unauthorized(new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Неверный пароль."
                    });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key_for_jwt_development_purposes_only"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: null,
                    audience: null,
                    claims: claims,
                    expires: DateTime.Now.AddDays(7),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                long jwtGenerationTime = stopwatch.ElapsedMilliseconds - dbQueryTime;
                Debug.WriteLine($"[AuthController] JWT Generation Duration: {jwtGenerationTime} ms");

                return Ok(new ApiResponse<AuthSuccessResponse>
                {
                    IsSuccess = true,
                    Data = new AuthSuccessResponse
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Role = user.Role,
                        Token = tokenString
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
            finally
            {
                stopwatch.Stop();
                Debug.WriteLine($"[AuthController] Total Login Method Duration: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, Message = "Неверный запрос: текущий или новый пароль пусты." });
                }

                var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
                }

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Пользователь не найден." });
                }

                if (user.Password != request.CurrentPassword)
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, Message = "Текущий пароль неверный." });
                }

                user.Password = request.NewPassword;
                await _dbContext.SaveChangesAsync();

                return Ok(new ApiResponse { IsSuccess = true, Message = "Пароль успешно изменен." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AuthController.ChangePassword: {ex.Message} - {ex.InnerException?.Message}");
                return StatusCode(500, new ApiResponse
                {
                    IsSuccess = false,
                    Message = $"Внутренняя ошибка сервера: {ex.Message}. Пожалуйста, попробуйте позже."
                });
            }
            finally
            {
                stopwatch.Stop();
                Debug.WriteLine($"[AuthController] Total ChangePassword Method Duration: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
    }
}

