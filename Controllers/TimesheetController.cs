using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/timesheet")]
    [ApiController]
    [Authorize]
    public class TimesheetController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public TimesheetController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimesheets([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] string worker, [FromQuery] string project, [FromQuery] string status)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var query = _dbContext.Timesheets
                .Include(t => t.User)
                .Where(t => t.UserId == userId);

            // Фильтрация по датам и другим параметрам
            if (DateTime.TryParse(startDate, out var start)) query = query.Where(t => t.Date >= start);
            if (DateTime.TryParse(endDate, out var end)) query = query.Where(t => t.Date <= end);
            if (!string.IsNullOrEmpty(worker)) query = query.Where(t => t.User.Username.Contains(worker));
            if (!string.IsNullOrEmpty(project)) query = query.Where(t => t.Project == project);
            if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);

            var timesheets = await query.Select(t => new
            {
                t.Id,
                Fio = t.User.Username, // Заменить на Profile.FullName, если доступно
                t.Project,
                t.Hours,
                t.BusinessTrip,
                t.Comment,
                t.Status,
                t.Date
            }).ToListAsync();

            return Ok(timesheets);
        }

        [HttpPost]
        public async Task<IActionResult> AddTimesheet([FromBody] Timesheet request)
        {
            if (request == null || request.Hours <= 0 || string.IsNullOrWhiteSpace(request.Project))
            {
                return BadRequest("Некорректные данные табеля");
            }

            request.UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "На рассмотрении";

            _dbContext.Timesheets.Add(request);
            await _dbContext.SaveChangesAsync();

            var user = await _dbContext.Users.FindAsync(request.UserId);
            var response = new
            {
                request.Id,
                Fio = user?.Username,
                request.Project,
                request.Hours,
                request.BusinessTrip,
                request.Comment,
                request.Status,
                request.Date
            };
            return Ok(response);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTimesheet(int id, [FromBody] Timesheet update)
        {
            var timesheet = await _dbContext.Timesheets.FindAsync(id);
            if (timesheet == null)
            {
                return NotFound("Табель не найден");
            }

            if (!string.IsNullOrEmpty(update.Status)) timesheet.Status = update.Status;

            await _dbContext.SaveChangesAsync();
            var user = await _dbContext.Users.FindAsync(timesheet.UserId);
            var response = new
            {
                timesheet.Id,
                Fio = user?.Username,
                timesheet.Project,
                timesheet.Hours,
                timesheet.BusinessTrip,
                timesheet.Comment,
                timesheet.Status,
                timesheet.Date
            };
            return Ok(response);
        }
    }
}
