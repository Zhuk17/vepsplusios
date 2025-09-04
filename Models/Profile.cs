using System;

namespace VepsPlusApi.Models
{
    public class Profile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public User User { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
