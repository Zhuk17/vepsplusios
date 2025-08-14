using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Для List
using System.Linq;
using System.Threading.Tasks;
using VepsPlusApi.Models; // Для Timesheet, User, AppDbContext, ApiResponse/ApiResponse<T>
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes

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
    [Authorize] // Added [Authorize] attribute
    public class TimesheetController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public TimesheetController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimesheets(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string worker, // Имя пользователя (Fio)
            [FromQuery] string project,
            [FromQuery] string status)
        {
            // ИСПРАВЛЕНИЕ: Получаем userId из JWT токена
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var query = _dbContext.Timesheets
                    .Include(t => t.User) // Загружаем связанные данные пользователя
                    .AsQueryable(); // Для добавления условий фильтрации

                // Фильтрация по датам (если предоставлены и корректны)
                if (DateTime.TryParse(startDate, out var start))
                {
                    query = query.Where(t => t.Date.Date >= start.Date);
                }
                if (DateTime.TryParse(endDate, out var end))
                {
                    query = query.Where(t => t.Date.Date <= end.Date);
                }

                // Фильтрация по работнику (Fio/Username)
                if (!string.IsNullOrEmpty(worker))
                {
                    query = query.Where(t => t.User.Username.Contains(worker));
                }
                // Фильтрация по проекту
                if (!string.IsNullOrEmpty(project))
                {
                    query = query.Where(t => t.Project.Contains(project)); // Изменено на Contains для частичного совпадения
                }
                // Фильтрация по статусу
                if (!string.IsNullOrEmpty(status))
                {
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

                return Ok(new ApiResponse<List<TimesheetResponseDto>> { IsSuccess = true, Data = timesheets, Message = "Табели успешно загружены." });
            }
            catch (Exception ex)
            {
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
