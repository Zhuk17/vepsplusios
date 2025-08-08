using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using VepsPlusApi.Models; // Для Settings, AppDbContext, и теперь для ApiResponse/ApiResponse<T>

namespace VepsPlusApi.Controllers
{
    // Универсальные модели ответа (находятся в VepsPlusApi/Models/ApiResponses.cs)
    // public class ApiResponse<T> { ... }
    // public class ApiResponse { ... }

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
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
            }

            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
                if (settings == null)
                {
                    // Если настроек нет, можем вернуть "Не найдено" или пустые дефолтные настройки
                    // Для удобства пользователя, лучше вернуть дефолтные
                    return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = new Settings { UserId = userId, DarkTheme = true, PushNotifications = true, Language = "ru", UpdatedAt = DateTime.UtcNow }, Message = "Настройки не найдены, возвращены стандартные." });
                }
                return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = settings, Message = "Настройки успешно загружены." });
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromQuery] int userId, [FromBody] Settings update)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
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
                    settings = new Settings { UserId = userId };
                    _dbContext.Settings.Add(settings);
                }

                // Обновление всех полей, так как это PUT (ожидается полный объект)
                settings.DarkTheme = update.DarkTheme;
                settings.PushNotifications = update.PushNotifications;
                settings.Language = update.Language ?? settings.Language; // В случае, если Language может прийти null
                settings.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                string message = isNewSettings ? "Настройки успешно созданы." : "Настройки успешно обновлены.";
                return Ok(new ApiResponse<Settings> { IsSuccess = true, Data = settings, Message = message });
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}