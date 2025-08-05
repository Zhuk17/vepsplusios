using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize]
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
            var userId = 1;
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();
            return Ok(notifications);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _dbContext.Notifications.FindAsync(id);
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