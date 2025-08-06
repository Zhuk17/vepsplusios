using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
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
                return BadRequest("Invalid user ID");
            }

            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();
            return Ok(notifications);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> MarkAsRead(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null)
            {
                return NotFound("Уведомление не найдено");
            }

            notification.IsRead = true;
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Уведомление отмечено как прочитанное" });
        }
    }
}