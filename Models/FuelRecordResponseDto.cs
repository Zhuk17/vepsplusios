using System;

namespace VepsPlusApi.Models
{
    public class FuelRecordResponseDto
    {
        public int Id { get; set; }
        public string Fio { get; set; } // ФИО из Profile
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public int Mileage { get; set; }
        public string FuelType { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; } // Гос. номер
        public DateTime CreatedAt { get; set; }
    }
}
