using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VepsPlusApi.Models;

namespace VepsPlusApi.Controllers
{
    [Route("api/v1/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public ProfileController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return NotFound("Профиль не найден");
            }
            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromQuery] int userId, [FromBody] Profile update)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID");
            }

            if (update == null)
            {
                return BadRequest("Invalid request body");
            }

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new Profile { UserId = userId };
                _dbContext.Profiles.Add(profile);
            }

            profile.FullName = update.FullName ?? profile.FullName;
            profile.Email = update.Email ?? profile.Email;
            profile.Phone = update.Phone ?? profile.Phone;
            profile.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return Ok(profile);
        }
    }
}