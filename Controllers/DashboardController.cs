using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public DashboardController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
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