using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class TimesheetUpdateRequest
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
