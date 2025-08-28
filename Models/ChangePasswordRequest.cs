using System.ComponentModel.DataAnnotations;

namespace VepsPlusApi.Models
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Текущий пароль обязателен.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен.")]
        [MinLength(4, ErrorMessage = "Новый пароль должен содержать минимум 4 символа.")]
        public string NewPassword { get; set; }
    }
}
