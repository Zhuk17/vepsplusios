using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VepsPlusApi.Extensions;

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
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
                if (settings == null)
                {
                    return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = new Settings { UserId = userId.Value, DarkTheme = true, PushNotifications = true, Language = "ru", UpdatedAt = DateTime.UtcNow }, Message = "Настройки не найдены, возвращены стандартные." });
                }
                return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = settings, Message = "Настройки успешно загружены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] Settings update)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            if (update == null)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный запрос: тело запроса пусто." });
            }

            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
                bool isNewSettings = (settings == null);

                if (isNewSettings)
                {
                    settings = new Settings { UserId = userId.Value };
                    _dbContext.Settings.Add(settings);
                }

                settings.DarkTheme = update.DarkTheme;
                settings.PushNotifications = update.PushNotifications;
                settings.Language = update.Language ?? settings.Language;
                settings.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                string message = isNewSettings ? "Настройки успешно созданы." : "Настройки успешно обновлены.";
                return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = settings, Message = message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}