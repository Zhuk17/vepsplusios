using System;
using System.Collections.Generic;

namespace VepsPlusApi.Models
{
    public class TimesheetResponseDto
    {
        public int Id { get; set; }
        public string Fio { get; set; }
        public string Project { get; set; }
        public int Hours { get; set; }
        public bool BusinessTrip { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }

    public class TimesheetCreateRequest
    {
        public DateTime Date { get; set; }
        public string Project { get; set; }
        public int Hours { get; set; }
        public bool BusinessTrip { get; set; }
        public string? Comment { get; set; }
    }

    public class TimesheetUpdateRequest
    {
        public string? Status { get; set; }
    }
}
