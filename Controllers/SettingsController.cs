using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/settings")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public SettingsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            // Извлекаем UserId из JWT-токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Неверный токен авторизации: не удалось определить пользователя.");
            }

            var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                return NotFound("Настройки не найдены");
            }
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] Settings update)
        {
            // Извлекаем UserId из JWT-токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Неверный токен авторизации: не удалось определить пользователя.");
            }

            var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                settings = new Settings { UserId = userId, UpdatedAt = DateTime.UtcNow };
                _dbContext.Settings.Add(settings);
            }

            settings.DarkTheme = update.DarkTheme;
            settings.PushNotifications = update.PushNotifications;
            // Используем ?? для Language, как в первом варианте, чтобы избежать перезаписи null
            settings.Language = update.Language ?? settings.Language;
            settings.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return Ok(settings);
        }
    }
}