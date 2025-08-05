using System;
using System.ComponentModel.DataAnnotations.Schema;
using VepsPlusApi.Models;

namespace VepsPlusApi.Models
{
    public class Timesheet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public DateTime Date { get; set; }
        public string Project { get; set; }
        public int Hours { get; set; }
        public bool BusinessTrip { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; } = "На рассмотрении";
        public DateTime CreatedAt { get; set; }
    }
}