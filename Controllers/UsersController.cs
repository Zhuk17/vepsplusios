using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VepsPlusApi.Extensions;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public UsersController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userIdClaim = this.GetUserId();
            if (userIdClaim == null || userIdClaim.Value != id)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Недостаточно прав для просмотра информации о другом пользователе." });
            }

            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Пользователь не найден." });
                }

                return Ok(new ApiResponse<User> { IsSuccess = true, Data = user, Message = "Данные пользователя успешно загружены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}
