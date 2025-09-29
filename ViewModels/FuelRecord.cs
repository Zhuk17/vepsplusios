using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class FuelRecord
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("fio")] public string? Fio { get; set; } // Made nullable
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("volume")] public decimal Volume { get; set; }
        [JsonPropertyName("cost")] public decimal Cost { get; set; }
        [JsonPropertyName("mileage")] public int Mileage { get; set; }
        [JsonPropertyName("fuelType")] public string? FuelType { get; set; } // Removed required
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("carModel")] public string? CarModel { get; set; } // Added CarModel
        [JsonPropertyName("licensePlate")] public string? LicensePlate { get; set; }

        public FuelRecord()
        {
            FuelType = string.Empty; // Initialize non-nullable string
        }
    }
}
