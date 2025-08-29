using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VepsPlusApi.Extensions;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/fuel")]
    [ApiController]
    [Authorize]
    public class FuelController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public FuelController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetFuelRecords()
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
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
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFuelRecord([FromBody] FuelRecord request)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            if (request == null || request.Volume <= 0 || request.Cost <= 0 || request.Mileage <= 0 || string.IsNullOrWhiteSpace(request.FuelType))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректные данные заправки. Проверьте объем, стоимость, пробег и тип топлива." });
            }

            var lastFuelRecord = await _dbContext.FuelRecords
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastFuelRecord != null && request.Mileage < lastFuelRecord.Mileage)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = $"Пробег ({request.Mileage} км) не может быть меньше предыдущего зафиксированного пробега ({lastFuelRecord.Mileage} км)." });
            }

            request.UserId = userId.Value;

            try
            {
                request.CreatedAt = DateTime.UtcNow;
                _dbContext.FuelRecords.Add(request);
                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse<FuelRecord> { IsSuccess = true, Data = request, Message = "Заправка успешно добавлена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFuelRecord(int id, [FromBody] FuelRecord update)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var record = await _dbContext.FuelRecords
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
                if (record == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Заправка не найдена." });
                }

                if (update.Date != default) record.Date = update.Date;
                if (update.Volume > 0) record.Volume = update.Volume;
                if (update.Cost > 0) record.Cost = update.Cost;
                if (update.Mileage > 0)
                {
                    var previousRecord = await _dbContext.FuelRecords
                        .Where(r => r.UserId == userId && r.Id != id)
                        .OrderByDescending(r => r.Date)
                        .ThenByDescending(r => r.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (previousRecord != null && update.Mileage < previousRecord.Mileage)
                    {
                        return BadRequest(new ApiResponse { IsSuccess = false, Message = $"Обновленный пробег ({update.Mileage} км) не может быть меньше предыдущего зафиксированного пробега ({previousRecord.Mileage} км)." });
                    }
                    else if (record.Mileage > 0 && update.Mileage < record.Mileage)
                    {
                         return BadRequest(new ApiResponse { IsSuccess = false, Message = $"Обновленный пробег ({update.Mileage} км) не может быть меньше текущего пробега записи ({record.Mileage} км)." });
                    }
                    record.Mileage = update.Mileage;
                }
                if (!string.IsNullOrWhiteSpace(update.FuelType)) record.FuelType = update.FuelType;

                await _dbContext.SaveChangesAsync();
                return Ok(new ApiResponse<FuelRecord> { IsSuccess = true, Data = record, Message = "Заправка успешно обновлена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuelRecord(int id)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
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
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}
