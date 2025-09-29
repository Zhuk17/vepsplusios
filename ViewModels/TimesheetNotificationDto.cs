using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class TimesheetNotificationDto
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("hours")]
        public int Hours { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }
}
