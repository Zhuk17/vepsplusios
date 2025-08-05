using System;

namespace VepsPlusApi.Models
{
    public class FuelRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public int Mileage { get; set; }
        public string FuelType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
