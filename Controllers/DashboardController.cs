using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/dashboard")]
    [ApiController]
    [Authorize] // Added [Authorize] attribute
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public DashboardController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            var totalHours = await _dbContext.Timesheets
                .Where(t => t.UserId == userId)
                .SumAsync(t => t.Hours);
            var totalFuelCost = await _dbContext.FuelRecords
                .Where(f => f.UserId == userId)
                .SumAsync(f => f.Cost);
            var totalMileage = await _dbContext.FuelRecords
                .Where(f => f.UserId == userId)
                .SumAsync(f => f.Mileage);
            var unreadNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            return Ok(new Dashboard
            {
                TotalHours = totalHours,
                TotalFuelCost = totalFuelCost,
                TotalMileage = totalMileage,
                UnreadNotifications = unreadNotifications
            });
        }
    }
}