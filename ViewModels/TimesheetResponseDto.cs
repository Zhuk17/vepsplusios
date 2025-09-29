using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class TimesheetResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fio")]
        public string? Fio { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("hours")]
        public int Hours { get; set; }

        [JsonPropertyName("businessTrip")]
        public bool BusinessTrip { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }
}
