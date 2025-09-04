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
        public async Task<IActionResult> GetFuelRecords(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var query = _dbContext.FuelRecords
                    .Include(r => r.User).ThenInclude(u => u.Profile) // Включаем User и Profile для получения Fio
                    .Where(r => r.UserId == userId)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(r => r.Date.Date >= startDate.Value.ToUniversalTime().Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(r => r.Date.Date <= endDate.Value.ToUniversalTime().Date);
                }

                var records = await query.Select(r => new FuelRecordResponseDto
                {
                    Id = r.Id,
                    Fio = r.User.Profile.FullName ?? r.User.Username ?? "Неизвестно",
                    Date = r.Date,
                    Volume = r.Volume,
                    Cost = r.Cost,
                    Mileage = r.Mileage,
                    FuelType = r.FuelType,
                    CarModel = r.CarModel,
                    LicensePlate = r.LicensePlate,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
                return Ok(new ApiResponse<List<FuelRecordResponseDto>> { IsSuccess = true, Data = records, Message = "Заправки успешно загружены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFuelRecord([FromBody] FuelRecordCreateRequest request)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            if (request == null || request.Volume <= 0 || request.Mileage <= 0 || string.IsNullOrWhiteSpace(request.FuelType) || string.IsNullOrWhiteSpace(request.LicensePlate))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректные данные заправки. Проверьте объем, пробег, тип топлива и гос. номер." });
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

            try
            {
                var newRecord = new FuelRecord
                {
                    UserId = userId.Value,
                    Date = request.Date.ToUniversalTime(),
                    Volume = request.Volume,
                    Cost = request.Volume * 50m, // Стоимость рассчитывается на сервере
                    Mileage = request.Mileage,
                    FuelType = request.FuelType,
                    CarModel = request.CarModel,
                    LicensePlate = request.LicensePlate,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.FuelRecords.Add(newRecord);
                await _dbContext.SaveChangesAsync();

                // Получаем FullName из Profile для ответа
                var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                var fio = userProfile?.FullName ?? _dbContext.Users.FirstOrDefault(u => u.Id == userId)?.Username ?? "Неизвестно";

                var responseDto = new FuelRecordResponseDto
                {
                    Id = newRecord.Id,
                    Fio = fio,
                    Date = newRecord.Date,
                    Volume = newRecord.Volume,
                    Cost = newRecord.Cost,
                    Mileage = newRecord.Mileage,
                    FuelType = newRecord.FuelType,
                    CarModel = newRecord.CarModel,
                    LicensePlate = newRecord.LicensePlate,
                    CreatedAt = newRecord.CreatedAt
                };

                return Ok(new ApiResponse<FuelRecordResponseDto> { IsSuccess = true, Data = responseDto, Message = "Заправка успешно добавлена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFuelRecord(int id, [FromBody] FuelRecordUpdateRequest update)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var record = await _dbContext.FuelRecords
                    .Include(r => r.User).ThenInclude(u => u.Profile)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
                if (record == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Заправка не найдена." });
                }

                if (update.Date.HasValue) record.Date = update.Date.Value.ToUniversalTime();
                if (update.Volume.HasValue && update.Volume > 0)
                {
                    record.Volume = update.Volume.Value;
                    record.Cost = update.Volume.Value * 50m; // Пересчитываем стоимость
                }
                else if (update.Volume.HasValue && update.Volume <= 0) // Если передали 0 или отрицательное, это ошибка
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, Message = "Объем топлива должен быть положительным." });
                }

                if (update.Mileage.HasValue && update.Mileage > 0)
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
                    record.Mileage = update.Mileage.Value;
                }
                else if (update.Mileage.HasValue && update.Mileage <= 0) // Если передали 0 или отрицательное, это ошибка
                {
                     return BadRequest(new ApiResponse { IsSuccess = false, Message = "Пробег должен быть положительным." });
                }

                if (!string.IsNullOrWhiteSpace(update.FuelType)) record.FuelType = update.FuelType;
                if (!string.IsNullOrWhiteSpace(update.CarModel)) record.CarModel = update.CarModel;
                if (!string.IsNullOrWhiteSpace(update.LicensePlate)) record.LicensePlate = update.LicensePlate;

                await _dbContext.SaveChangesAsync();

                // Получаем FullName из Profile для ответа
                var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                var fio = userProfile?.FullName ?? record.User.Username ?? "Неизвестно";

                var responseDto = new FuelRecordResponseDto
                {
                    Id = record.Id,
                    Fio = fio,
                    Date = record.Date,
                    Volume = record.Volume,
                    Cost = record.Cost,
                    Mileage = record.Mileage,
                    FuelType = record.FuelType,
                    CarModel = record.CarModel,
                    LicensePlate = record.LicensePlate,
                    CreatedAt = record.CreatedAt
                };

                return Ok(new ApiResponse<FuelRecordResponseDto> { IsSuccess = true, Data = responseDto, Message = "Заправка успешно обновлена." });
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
