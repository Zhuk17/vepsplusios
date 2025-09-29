using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class FuelRecordResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("fio")]
        public string? Fio { get; set; } // Nullable, так как может быть не загружен

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; set; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        [JsonPropertyName("mileage")]
        public int Mileage { get; set; }

        [JsonPropertyName("fuelType")]
        public string? FuelType { get; set; } // Removed required

        [JsonPropertyName("licensePlate")]
        public string? LicensePlate { get; set; } // Nullable

        [JsonPropertyName("carModel")]
        public string? CarModel { get; set; } // Nullable

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        public FuelRecordResponseDto()
        {
            FuelType = string.Empty;
        }
    }
}
