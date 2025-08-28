using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes
using VepsPlusApi.Extensions; // ДОБАВЛЕНО: Для GetUserId()

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
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            var dashboardData = await _dbContext.Users // Начинаем с Users, чтобы гарантировать, что userId существует
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    TotalHours = _dbContext.Timesheets.Where(t => t.UserId == userId).Sum(t => t.Hours),
                    TotalFuelCost = _dbContext.FuelRecords.Where(f => f.UserId == userId).Sum(f => f.Cost),
                    TotalMileage = _dbContext.FuelRecords.Where(f => f.UserId == userId).Sum(f => f.Mileage),
                    UnreadNotifications = _dbContext.Notifications.Where(n => n.UserId == userId && !n.IsRead).Count()
                })
                .FirstOrDefaultAsync();

            if (dashboardData == null)
            {
                // Если пользователь не найден (хотя GetUserId() уже проверил), или нет данных
                return NotFound(new ApiResponse { IsSuccess = false, Message = "Данные для дашборда не найдены." });
            }

            return Ok(new Dashboard
            {
                TotalHours = dashboardData.TotalHours,
                TotalFuelCost = dashboardData.TotalFuelCost,
                TotalMileage = dashboardData.TotalMileage,
                UnreadNotifications = dashboardData.UnreadNotifications
            });
        }
    }
}