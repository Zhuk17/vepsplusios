using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class FuelRecordUpdateRequest
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("volume")]
        public decimal? Volume { get; set; }

        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }

        [JsonPropertyName("mileage")]
        public int? Mileage { get; set; }

        [JsonPropertyName("fuelType")]
        public string? FuelType { get; set; }

        [JsonPropertyName("carModel")]
        public string? CarModel { get; set; }

        [JsonPropertyName("licensePlate")]
        public string? LicensePlate { get; set; }
    }
}
