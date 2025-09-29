using System;
using System.Text.Json.Serialization;

namespace VEPS_Plus.ViewModels
{
    public class Notification
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } = "general"; // Тип уведомления

        public Notification()
        {
            Title = string.Empty;
            Message = string.Empty;
        }
    }
}
