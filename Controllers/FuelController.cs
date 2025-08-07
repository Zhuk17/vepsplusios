using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models; // Убедитесь, что FuelRecord и AppDbContext здесь

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/fuel")]
    [ApiController]
    public class FuelController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public FuelController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetFuelRecords([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
            }

            try
            {
                var records = await _dbContext.FuelRecords
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                return Ok(new ApiResponse<List<FuelRecord>> { IsSuccess = true, Data = records, Message = "Заправки успешно загружены." });
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь должно быть логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFuelRecord([FromBody] FuelRecord request)
        {
            // Note: Request.UserId будет 0, если не передан или не соответствует типу.
            // Проверка на request.UserId <= 0 должна быть адекватной для этого.
            if (request == null || request.UserId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя или пустой запрос." });
            }

            if (request.Volume <= 0 || request.Cost <= 0 || request.Mileage <= 0 || string.IsNullOrWhiteSpace(request.FuelType))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректные данные заправки. Проверьте объем, стоимость, пробег и тип топлива." });
            }

            try
            {
                request.CreatedAt = DateTime.UtcNow; // Установка даты создания
                _dbContext.FuelRecords.Add(request);
                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse<FuelRecord> { IsSuccess = true, Data = request, Message = "Заправка успешно добавлена." });
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь должно быть логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFuelRecord(int id, [FromQuery] int userId, [FromBody] FuelRecord update)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
            }

            try
            {
                var record = await _dbContext.FuelRecords
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
                if (record == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Заправка не найдена." });
                }

                // Обновление полей только если они предоставлены и валидны
                if (update.Date != default) record.Date = update.Date;
                if (update.Volume > 0) record.Volume = update.Volume;
                if (update.Cost > 0) record.Cost = update.Cost;
                if (update.Mileage > 0) record.Mileage = update.Mileage;
                if (!string.IsNullOrWhiteSpace(update.FuelType)) record.FuelType = update.FuelType;

                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse<FuelRecord> { IsSuccess = true, Data = record, Message = "Заправка успешно обновлена." });
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь должно быть логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuelRecord(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный ID пользователя." });
            }

            try
            {
                var record = await _dbContext.FuelRecords
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
                if (record == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Заправка не найдена." });
                }

                _dbContext.FuelRecords.Remove(record);
                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse { IsSuccess = true, Message = "Заправка успешно удалена." });
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь должно быть логирование ошибки
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}
