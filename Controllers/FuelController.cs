using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

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
            // TODO: Получить UserId из JWT-токена
            var userId = 1; // Заглушка для теста
            var records = await _dbContext.FuelRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();
            return Ok(records);
        }

        [HttpPost]
        public async Task<IActionResult> AddFuelRecord([FromBody] FuelRecord request)
        {
            if (request.Volume <= 0 || request.Cost <= 0 || request.Mileage <= 0 || string.IsNullOrWhiteSpace(request.FuelType))
            {
                return BadRequest("Некорректные данные заправки");
            }

            // TODO: Получить UserId из JWT-токена
            request.UserId = 1; // Заглушка для теста
            request.CreatedAt = DateTime.UtcNow;

            _dbContext.FuelRecords.Add(request);
            await _dbContext.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFuelRecord(int id, [FromBody] FuelRecord update)
        {
            var record = await _dbContext.FuelRecords.FindAsync(id);
            if (record == null)
            {
                return NotFound("Заправка не найдена");
            }

            // TODO: Проверить, что UserId из токена соответствует record.UserId
            if (update.Date != default) record.Date = update.Date;
            if (update.Volume > 0) record.Volume = update.Volume;
            if (update.Cost > 0) record.Cost = update.Cost;
            if (update.Mileage > 0) record.Mileage = update.Mileage;
            if (!string.IsNullOrWhiteSpace(update.FuelType)) record.FuelType = update.FuelType;

            await _dbContext.SaveChangesAsync();
            return Ok(record);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuelRecord(int id)
        {
            var record = await _dbContext.FuelRecords.FindAsync(id);
            if (record == null)
            {
                return NotFound("Заправка не найдена");
            }

            // TODO: Проверить, что UserId из токена соответствует record.UserId
            _dbContext.FuelRecords.Remove(record);
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Заправка удалена" });
        }
    }
}
