using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;
using Microsoft.Extensions.Logging;
using VepsPlusApi.Models.TimesheetDtos; // Используем DTO из отдельного файла

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/timesheets")]
    [ApiController]
    [Authorize]
    public class TimesheetController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TimesheetController> _logger;

        public TimesheetController(AppDbContext dbContext, ILogger<TimesheetController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimesheets(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? worker,
            [FromQuery] string? project,
            [FromQuery] string? status)
        {
            _logger.LogInformation($"[TimesheetController] GetTimesheets called. startDate: {startDate}, endDate: {endDate}, worker: {worker}, project: {project}, status: {status}");

            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("[TimesheetController] Unauthorized: User ID claim missing or invalid.");
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }
            _logger.LogInformation($"[TimesheetController] Authenticated UserId: {userId}");

            try
            {
                var query = _dbContext.Timesheets
                    .Include(t => t.User)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    _logger.LogInformation($"[TimesheetController] Parsed startDate: {startDate.Value.Date:yyyy-MM-dd}");
                    query = query.Where(t => t.Date.Date >= startDate.Value.Date.ToUniversalTime());
                }
                else
                {
                    _logger.LogWarning($"[TimesheetController] startDate is null or failed to parse.");
                }
                if (endDate.HasValue)
                {
                    _logger.LogInformation($"[TimesheetController] Parsed endDate: {endDate.Value.Date:yyyy-MM-dd}");
                    query = query.Where(t => t.Date.Date <= endDate.Value.Date.ToUniversalTime());
                }
                else
                {
                    _logger.LogWarning($"[TimesheetController] endDate is null or failed to parse.");
                }

                if (!string.IsNullOrEmpty(worker))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by worker: {worker}");
                    query = query.Where(t => t.User.Username.Contains(worker));
                }
                if (!string.IsNullOrEmpty(project))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by project: {project}");
                    query = query.Where(t => t.Project.Contains(project));
                }
                if (!string.IsNullOrEmpty(status))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by status: {status}");
                    query = query.Where(t => t.Status == status);
                }

                query = query.Where(t => t.UserId == userId);
                _logger.LogInformation($"[TimesheetController] Applying UserID filter: {userId}");

                var timesheets = await query.Select(t => new TimesheetResponseDto
                {
                    Id = t.Id,
                    Fio = t.User.Username,
                    Project = t.Project,
                    Hours = t.Hours,
                    BusinessTrip = t.BusinessTrip,
                    Comment = t.Comment,
                    Status = t.Status,
                    Date = t.Date
                }).ToListAsync();
                _logger.LogInformation($"[TimesheetController] Retrieved {timesheets.Count} timesheets.");

                return Ok(new ApiResponse<List<TimesheetResponseDto>> { IsSuccess = true, Data = timesheets, Message = "Табели успешно загружены." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TimesheetController] An error occurred while getting timesheets.");
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTimesheet([FromBody] TimesheetCreateRequest request)
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            if (request == null || request.Hours <= 0 || string.IsNullOrWhiteSpace(request.Project))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректные данные табеля: часы и проект обязательны." });
            }

            try
            {
                var timesheet = new Timesheet
                {
                    UserId = userId,
                    Date = request.Date.ToUniversalTime(), // Преобразовать в UTC
                    Project = request.Project,
                    Hours = request.Hours,
                    BusinessTrip = request.BusinessTrip,
                    Comment = request.Comment,
                    Status = "На рассмотрении",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Timesheets.Add(timesheet);
                await _dbContext.SaveChangesAsync();

                var createdTimesheet = await _dbContext.Timesheets
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == timesheet.Id);

                var responseDto = new TimesheetResponseDto
                {
                    Id = createdTimesheet.Id,
                    Fio = createdTimesheet.User?.Username,
                    Project = createdTimesheet.Project,
                    Hours = createdTimesheet.Hours,
                    BusinessTrip = createdTimesheet.BusinessTrip,
                    Comment = createdTimesheet.Comment,
                    Status = createdTimesheet.Status,
                    Date = createdTimesheet.Date
                };
                return Ok(new ApiResponse<TimesheetResponseDto> { IsSuccess = true, Data = responseDto, Message = "Табель успешно добавлен." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTimesheet(int id, [FromBody] TimesheetUpdateRequest update)
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var timesheet = await _dbContext.Timesheets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
                if (timesheet == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Табель не найден." });
                }

                if (!string.IsNullOrEmpty(update.Status)) timesheet.Status = update.Status;

                await _dbContext.SaveChangesAsync();

                var updatedTimesheet = await _dbContext.Timesheets
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == timesheet.Id);

                var responseDto = new TimesheetResponseDto
                {
                    Id = updatedTimesheet.Id,
                    Fio = updatedTimesheet.User?.Username,
                    Project = updatedTimesheet.Project,
                    Hours = updatedTimesheet.Hours,
                    BusinessTrip = updatedTimesheet.BusinessTrip,
                    Comment = updatedTimesheet.Comment,
                    Status = updatedTimesheet.Status,
                    Date = updatedTimesheet.Date
                };
                return Ok(new ApiResponse<TimesheetResponseDto> { IsSuccess = true, Data = responseDto, Message = "Табель успешно обновлен." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}
