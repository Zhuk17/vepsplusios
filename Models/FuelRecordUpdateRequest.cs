using System;
using System.ComponentModel.DataAnnotations;

namespace VepsPlusApi.Models
{
    public class FuelRecordUpdateRequest
    {
        public DateTime? Date { get; set; }
        public decimal? Volume { get; set; }
        public decimal? Cost { get; set; }
        public int? Mileage { get; set; }
        [StringLength(50, ErrorMessage = "Тип топлива не может превышать 50 символов.")]
        public string? FuelType { get; set; }
        [StringLength(50, ErrorMessage = "Модель автомобиля не может превышать 50 символов.")]
        public string? CarModel { get; set; }
        [StringLength(20, ErrorMessage = "Гос. номер не может превышать 20 символов.")]
        public string? LicensePlate { get; set; }
    }
}
