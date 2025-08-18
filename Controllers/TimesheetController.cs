using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Для List
using System.Linq;
using System.Threading.Tasks;
using VepsPlusApi.Models; // Для Timesheet, User, AppDbContext, ApiResponse/ApiResponse<T>
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes
using System.Globalization; // Added for CultureInfo and DateTimeStyles
using Microsoft.Extensions.Logging; // ДОБАВЛЕНО: Для логирования

namespace VepsPlusApi.Controllers
{
    // Модель для данных табеля, возвращаемых клиенту
    public class TimesheetResponseDto
    {
        public int Id { get; set; }
        public string Fio { get; set; } // ФИО работника
        public string Project { get; set; }
        public int Hours { get; set; }
        public bool BusinessTrip { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }

    [Route("api/v1/timesheet")]
    [ApiController]
    [Authorize] // Added [Authorize] attribute - Вернул обратно, так как проблема не в нем
    public class TimesheetController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TimesheetController> _logger; // ДОБАВЛЕНО: Для логирования

        public TimesheetController(AppDbContext dbContext, ILogger<TimesheetController> logger) // ИЗМЕНЕНО: Добавлен ILogger
        {
            _dbContext = dbContext;
            _logger = logger; // ДОБАВЛЕНО: Инициализация логгера
        }

        [HttpGet]
        public async Task<IActionResult> GetTimesheets(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? worker, // ИЗМЕНЕНО: Сделано необязательным
            [FromQuery] string? project,
            [FromQuery] string? status)
        {
            _logger.LogInformation($"[TimesheetController] GetTimesheets called. startDate: {startDate}, endDate: {endDate}, worker: {worker}, project: {project}, status: {status}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ ВХОДНЫХ ПАРАМЕТРОВ

            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("[TimesheetController] Unauthorized: User ID claim missing or invalid."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }
            _logger.LogInformation($"[TimesheetController] Authenticated UserId: {userId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ

            try
            {
                var query = _dbContext.Timesheets
                    .Include(t => t.User) // Загружаем связанные данные пользователя
                    .AsQueryable(); // Для добавления условий фильтрации

                // Фильтрация по датам (если предоставлены и корректны)
                if (startDate.HasValue)
                {
                    _logger.LogInformation($"[TimesheetController] Parsed startDate: {startDate.Value.Date:yyyy-MM-dd}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    query = query.Where(t => t.Date.Date >= startDate.Value.Date.ToUniversalTime()); // ИЗМЕНЕНО: Преобразование в UTC
                }
                else
                {
                    _logger.LogWarning($"[TimesheetController] startDate is null or failed to parse."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                }
                if (endDate.HasValue)
                {
                    _logger.LogInformation($"[TimesheetController] Parsed endDate: {endDate.Value.Date:yyyy-MM-dd}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    query = query.Where(t => t.Date.Date <= endDate.Value.Date.ToUniversalTime()); // ИЗМЕНЕНО: Преобразование в UTC
                }
                else
                {
                    _logger.LogWarning($"[TimesheetController] endDate is null or failed to parse."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                }

                // Фильтрация по работнику (Fio/Username)
                if (!string.IsNullOrEmpty(worker))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by worker: {worker}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    query = query.Where(t => t.User.Username.Contains(worker));
                }
                // Фильтрация по проекту
                if (!string.IsNullOrEmpty(project))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by project: {project}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    query = query.Where(t => t.Project.Contains(project)); // Изменено на Contains для частичного совпадения
                }
                // Фильтрация по статусу
                if (!string.IsNullOrEmpty(status))
                {
                    _logger.LogInformation($"[TimesheetController] Filtering by status: {status}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    query = query.Where(t => t.Status == status);
                }

                // ВНИМАНИЕ: Если userId предназначен для фильтрации ТОЛЬКО своих записей, добавьте:
                // query = query.Where(t => t.UserId == userId);
                // Если же это для менеджера, чтобы получить все табели, но фильтровать по userId,
                // то это более сложная логика ролей.
                // Для простоты, если не указан worker, то возвращаем ВСЕ табели, доступные текущему пользователю
                // или все, если пользователь - менеджер. Если worker указан, это фильтр.
                // Для этого примера, давайте предположим, что userId - это кто запрашивает, и он видит только свои
                // или если worker указан, это фильтр для этого worker.
                // Для MVP: Запрашивающий userId видит ТОЛЬКО свои табели.
                query = query.Where(t => t.UserId == userId); // Показываем только табели текущего пользователя
                _logger.LogInformation($"[TimesheetController] Applying UserID filter: {userId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ

                var timesheets = await query.Select(t => new TimesheetResponseDto // Проецируем в DTO
                {
                    Id = t.Id,
                    Fio = t.User.Username, // Используем Username пользователя
                    Project = t.Project,
                    Hours = t.Hours,
                    BusinessTrip = t.BusinessTrip,
                    Comment = t.Comment,
                    Status = t.Status,
                    Date = t.Date
                }).ToListAsync();
                _logger.LogInformation($"[TimesheetController] Retrieved {timesheets.Count} timesheets."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ

                return Ok(new ApiResponse<List<TimesheetResponseDto>> { IsSuccess = true, Data = timesheets, Message = "Табели успешно загружены." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TimesheetController] An error occurred while getting timesheets."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ ОШИБОК
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTimesheet([FromBody] Timesheet request)
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена и устанавливаем его
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
                request.UserId = userId;
                request.CreatedAt = DateTime.UtcNow;
                request.Status = "На рассмотрении"; // Дефолтный статус при добавлении

                _dbContext.Timesheets.Add(request);
                await _dbContext.SaveChangesAsync();

                var user = await _dbContext.Users.FindAsync(request.UserId);
                var responseDto = new TimesheetResponseDto // Проецируем в DTO
                {
                    Id = request.Id,
                    Fio = user?.Username,
                    Project = request.Project,
                    Hours = request.Hours,
                    BusinessTrip = request.BusinessTrip,
                    Comment = request.Comment,
                    Status = request.Status,
                    Date = request.Date
                };
                return Ok(new ApiResponse<TimesheetResponseDto> { IsSuccess = true, Data = responseDto, Message = "Табель успешно добавлен." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTimesheet(int id, [FromBody] Timesheet update)
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
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

                // Обновляем только разрешенные поля (например, статус для менеджера)
                // Если обновлять нужно больше полей, их нужно добавить сюда.
                if (!string.IsNullOrEmpty(update.Status)) timesheet.Status = update.Status;
                // Для других полей:
                // if (update.Hours > 0) timesheet.Hours = update.Hours;
                // if (update.Project != null) timesheet.Project = update.Project;
                // ...

                await _dbContext.SaveChangesAsync();

                var user = await _dbContext.Users.FindAsync(timesheet.UserId);
                var responseDto = new TimesheetResponseDto // Проецируем в DTO
                {
                    Id = timesheet.Id,
                    Fio = user?.Username,
                    Project = timesheet.Project,
                    Hours = timesheet.Hours,
                    BusinessTrip = timesheet.BusinessTrip,
                    Comment = timesheet.Comment,
                    Status = timesheet.Status,
                    Date = timesheet.Date
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
