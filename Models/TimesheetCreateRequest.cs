using System;
using System.ComponentModel.DataAnnotations;

namespace VepsPlusApi.Models
{
    public class TimesheetCreateRequest
    {
        [Required(ErrorMessage = "Дата обязательна.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Проект обязателен.")]
        [StringLength(100, ErrorMessage = "Название проекта не может превышать 100 символов.")]
        public string Project { get; set; }

        [Required(ErrorMessage = "Часы обязательны.")]
        [Range(1, 24, ErrorMessage = "Часы должны быть от 1 до 24.")]
        public int Hours { get; set; }

        public bool BusinessTrip { get; set; }

        [StringLength(500, ErrorMessage = "Комментарий не может превышать 500 символов.")]
        public string? Comment { get; set; }
    }
}
