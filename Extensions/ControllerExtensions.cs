using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace VepsPlusApi.Extensions
{
    public static class ControllerExtensions
    {
        public static int? GetUserId(this ControllerBase controller)
        {
            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return null;
            }
            return userId;
        }
    }
}
