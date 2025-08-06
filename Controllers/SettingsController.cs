using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public SettingsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                return NotFound("Настройки не найдены");
            }
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromQuery] int userId, [FromBody] Settings update)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                settings = new Settings { UserId = userId, UpdatedAt = DateTime.UtcNow };
                _dbContext.Settings.Add(settings);
            }

            settings.DarkTheme = update.DarkTheme;
            settings.PushNotifications = update.PushNotifications;
            settings.Language = update.Language ?? settings.Language;
            settings.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return Ok(settings);
        }
    }
}