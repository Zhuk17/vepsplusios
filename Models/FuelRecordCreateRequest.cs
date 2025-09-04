using System;
using System.ComponentModel.DataAnnotations;

namespace VepsPlusApi.Models
{
    public class FuelRecordCreateRequest
    {
        [Required(ErrorMessage = "Дата обязательна.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Объем топлива обязателен.")]
        [Range(0.01, 1000.00, ErrorMessage = "Объем должен быть от 0.01 до 1000.")]
        public decimal Volume { get; set; }

        // Стоимость будет рассчитываться на сервере, но для DTO она может быть опциональной или не передаваться
        // [Required(ErrorMessage = "Стоимость обязательна.")]
        // [Range(0.01, 100000.00, ErrorMessage = "Стоимость должна быть от 0.01 до 100000.")]
        // public decimal Cost { get; set; }

        [Required(ErrorMessage = "Пробег обязателен.")]
        [Range(1, 1000000, ErrorMessage = "Пробег должен быть от 1 до 1000000.")]
        public int Mileage { get; set; }

        [Required(ErrorMessage = "Тип топлива обязателен.")]
        [StringLength(50, ErrorMessage = "Тип топлива не может превышать 50 символов.")]
        public string FuelType { get; set; }

        [StringLength(50, ErrorMessage = "Модель автомобиля не может превышать 50 символов.")]
        public string? CarModel { get; set; }

        [Required(ErrorMessage = "Гос. номер обязателен.")]
        [StringLength(20, ErrorMessage = "Гос. номер не может превышать 20 символов.")]
        public string LicensePlate { get; set; }
    }
}
