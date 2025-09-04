using System;
using System.Collections.Generic;

namespace VepsPlusApi.Models
{
    public class FuelRecordResponseDto
    {
        public int Id { get; set; }
        public string Fio { get; set; }
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public int Mileage { get; set; }
        public string FuelType { get; set; }
        public string? LicensePlate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FuelRecordCreateRequest
    {
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public int Mileage { get; set; }
        public string FuelType { get; set; }
        public string? LicensePlate { get; set; }
    }

    public class FuelRecordUpdateRequest
    {
        public DateTime? Date { get; set; }
        public decimal? Volume { get; set; }
        public int? Mileage { get; set; }
        public string? FuelType { get; set; }
        public string? LicensePlate { get; set; }
    }
}
