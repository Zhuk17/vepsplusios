using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models; // Для Notification, AppDbContext, и теперь для ApiResponse/ApiResponse<T>

namespace VepsPlusApi.Controllers
{
    // Универсальные модели ответа (находятся в VepsPlusApi/Models/ApiResponses.cs)
    // public class ApiResponse<T> { ... }
    // public class ApiResponse { ... }

    [Route("api/v1/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public NotificationsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
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
        public async Task<IActionResult> MarkAsRead(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
            }

            try
            {
                var notification = await _dbContext.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
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