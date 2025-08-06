using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

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
                return BadRequest("Invalid user ID");
            }

            var records = await _dbContext.FuelRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();
            return Ok(records);
        }

        [HttpPost]
        public async Task<IActionResult> AddFuelRecord([FromBody] FuelRecord request)
        {
            if (request == null || request.UserId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            if (request.Volume <= 0 || request.Cost <= 0 || request.Mileage <= 0 || string.IsNullOrWhiteSpace(request.FuelType))
            {
                return BadRequest("Некорректные данные заправки");
            }

            request.CreatedAt = DateTime.UtcNow;

            _dbContext.FuelRecords.Add(request);
            await _dbContext.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFuelRecord(int id, [FromQuery] int userId, [FromBody] FuelRecord update)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var record = await _dbContext.FuelRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (record == null)
            {
                return NotFound("Заправка не найдена");
            }

            if (update.Date != default) record.Date = update.Date;
            if (update.Volume > 0) record.Volume = update.Volume;
            if (update.Cost > 0) record.Cost = update.Cost;
            if (update.Mileage > 0) record.Mileage = update.Mileage;
            if (!string.IsNullOrWhiteSpace(update.FuelType)) record.FuelType = update.FuelType;

            await _dbContext.SaveChangesAsync();
            return Ok(record);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuelRecord(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var record = await _dbContext.FuelRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (record == null)
            {
                return NotFound("Заправка не найдена");
            }

            _dbContext.FuelRecords.Remove(record);
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Заправка удалена" });
        }
    }
}
