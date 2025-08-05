using System;

namespace VepsPlusApi.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool DarkTheme { get; set; }
        public bool PushNotifications { get; set; }
        public string Language { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
