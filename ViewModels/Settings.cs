using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class Settings
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("darkTheme")]
        public bool DarkTheme { get; set; }

        [JsonPropertyName("pushNotifications")]
        public bool PushNotifications { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        public Settings()
        {
            Language = string.Empty;
        }
    }
}
