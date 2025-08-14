using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models; // Для Notification, AppDbContext, и теперь для ApiResponse/ApiResponse<T>
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes

namespace VepsPlusApi.Controllers
{
    // Универсальные модели ответа (находятся в VepsPlusApi/Models/ApiResponses.cs)
    // public class ApiResponse<T> { ... }
    // public class ApiResponse { ... }

    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize] // Added [Authorize] attribute
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public NotificationsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();
                return Ok(new ApiResponse<List<Notification>> { IsSuccess = true, Data = notifications, Message = "Уведомления успешно загружены." });
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var notification = await _dbContext.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId); // Проверяем принадлежность уведомления пользователю
                if (notification == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Уведомление не найдено." });
                }

                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse { IsSuccess = true, Message = "Уведомление отмечено как прочитанное." });
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}