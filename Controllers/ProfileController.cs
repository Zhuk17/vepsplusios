using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VepsPlusApi.Extensions;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public ProfileController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            try
            {
                var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    return NotFound(new ApiResponse { IsSuccess = false, Message = "Профиль не найден." });
                }
                return Ok(new ApiResponse<Profile> { IsSuccess = true, Data = profile, Message = "Профиль успешно загружен." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] Profile update)
        {
            var userId = this.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Пользователь не авторизован или User ID не найден в токене." });
            }

            if (update == null)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, Message = "Некорректный запрос: тело запроса пусто." });
            }

            try
            {
                var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    profile = new Profile { UserId = userId.Value };
                    _dbContext.Profiles.Add(profile);
                }

                if (update.FullName != null) profile.FullName = update.FullName;
                if (update.Email != null) profile.Email = update.Email;
                if (update.Phone != null) profile.Phone = update.Phone;
                
                profile.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                string message = (profile.Id == 0) ? "Профиль успешно создан." : "Профиль успешно обновлен.";

                return Ok(new ApiResponse<Profile> { IsSuccess = true, Data = profile, Message = message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { IsSuccess = false, Message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}