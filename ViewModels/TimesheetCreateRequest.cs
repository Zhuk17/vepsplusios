using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class TimesheetCreateRequest
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("hours")]
        public int Hours { get; set; }

        [JsonPropertyName("businessTrip")]
        public bool BusinessTrip { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        public TimesheetCreateRequest()
        {
            Project = string.Empty;
        }
    }
}
